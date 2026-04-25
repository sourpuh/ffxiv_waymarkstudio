using Lumina.Data.Files;
using Lumina.Data.Parsing.Layer;
using System;
using System.Collections.Generic;
using System.Linq;
using WaymarkStudio.Maps;

namespace WaymarkStudio;

using static Lumina.Data.Parsing.Layer.LayerCommon;
using ContentTypeInfo = (uint icon, string name);
using ExpansionInfo = (uint icon, string name);
using LuminaContentType = Lumina.Excel.Sheets.ContentType;
using TerritoryType = Lumina.Excel.Sheets.TerritoryType;
using ExVersion = Lumina.Excel.Sheets.ExVersion;

internal static class TerritorySheet
{
    private static Dictionary<Expansion, ExpansionInfo> ExpansionInfos;
    private static Dictionary<ContentType, ContentTypeInfo> ContentTypeInfos;
    private static ILookup<ushort, ushort> AlternativeTerritoryIds;

    internal readonly struct TerritoryInfo
    {
        internal ushort Id { get; init; }
        internal string Name { get; init; }
        internal ushort ContentId { get; init; }
        internal Expansion Expansion { get; init; }
        internal ContentType ContentType { get; init; }
        internal string Bg { get; init; }
        internal uint MapId { get; init; }
        internal bool AreWaymarksSupported { get; init; }
        internal bool ArePresetsSupported { get; init; }
    }
    private static Dictionary<ushort, TerritoryInfo> TerritoryIdToInfo;
    private static Dictionary<string, IEnumerable<MapRange>> BgToMapRangeCache = new();

    static TerritorySheet()
    {
        TerritoryIdToInfo = Plugin.DataManager.GetExcelSheet<TerritoryType>()
            .Where(x=> !x.TerritoryIntendedUse.Value.DisableFieldMarkers)
            .ToDictionary(x => (ushort)x.RowId,
            x => new TerritoryInfo()
            {
                Id = (ushort)x.RowId,
                Name = x.GetName(),
                ContentId = (ushort)x.GetContentId(),
                Expansion = (Expansion)x.ExVersion.RowId,
                ContentType = GetContentType(x),
                Bg = x.Bg.ExtractText(),
                MapId = x.Map.RowId,
                AreWaymarksSupported = !x.TerritoryIntendedUse.Value.DisableFieldMarkers,
                ArePresetsSupported = x.TerritoryIntendedUse.Value.EnableFieldMarkerPresets,
            });

        AlternativeTerritoryIds = TerritoryIdToInfo.Values
            .Where(t => t.ContentType == ContentType.VCDungeonFinder)
            .GroupBy(t => t.Bg)
            .SelectMany(g => g.SelectMany(t => g.Select(alt => (Key: t.Id, Value: alt.Id))))
            .ToLookup(x => x.Key, x => x.Value);

        ExpansionInfos = new();
        var exVersion = Plugin.DataManager.GetExcelSheet<ExVersion>();
        foreach (var expansion in Enum.GetValues<Expansion>())
        {
            var luminaExpansion = exVersion.GetRow((uint)expansion);
            ExpansionInfos.Add(expansion, (61875 + (uint)expansion, luminaExpansion.Name.ExtractText()));
        }

        var contentTypeSheet = Plugin.DataManager.GetExcelSheet<LuminaContentType>();
        ContentTypeInfos = new();
        foreach (var contentType in Enum.GetValues<ContentType>())
        {
            switch (contentType)
            {
                case ContentType.OpenWorld:
                    ContentTypeInfos.Add(contentType, (61844, "Open World"));
                    break;
                case ContentType.Other:
                    ContentTypeInfos.Add(contentType, (61831, "Other"));
                    break;
                default:
                    var luminaContentType = contentTypeSheet.GetRow((uint)contentType);
                    ContentTypeInfos.Add(contentType, (luminaContentType.Icon, luminaContentType.Name.ExtractText()));
                    break;
            }
        }
    }

    public static IEnumerable<(uint, HashSet<Waymark>)> GetPresetMapIds(WaymarkPreset preset)
    {
        var territory = TerritoryIdToInfo[preset.TerritoryId];
        var ranges = GetMapRanges(territory);

        Dictionary<uint, HashSet<Waymark>> mapWaymarks = [];
        foreach (var range in ranges)
        {
            var waymarks = preset.MarkerPositions.Where(x => range.Contains(x.Value)).Select(x => x.Key).ToHashSet();
            if (waymarks.Any())
                if (mapWaymarks.TryGetValue(range.MapId, out var waymarkSet))
                    waymarkSet.UnionWith(waymarks);
                else
                    mapWaymarks[range.MapId] = waymarks;
        }

        // I don't know how the game chooses which map to display when multiple maps are viable for the same waymark.
        // Lumina maps have a territory ID which does not necessarily match the territory ID they are used on.
        // This is an approximation that is close enough to ingame behavior. M8N is the only territory I know of where this will use the wrong map.
        // If there are multiple map ranges containing the same set of waymarks, arbitrarily keep the map range with the highest ID value.
        mapWaymarks = mapWaymarks
            .GroupBy(kvp => kvp.Value, HashSet<Waymark>.CreateSetComparer())
            .Select(g => g.MaxBy(kvp => kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var mappedWaymarks = mapWaymarks.Select(x => x.Value).SelectMany(x => x).ToHashSet();
        var unmappedWaymarks = preset.MarkerPositions.Keys.Where(x => !mappedWaymarks.Contains(x));

        // If there was no MapRange containing the waymarks, return the default map.
        if (unmappedWaymarks.Any())
        {
            var defaultMapId = territory.MapId;
            mapWaymarks[defaultMapId] = preset.MarkerPositions.Keys.ToHashSet();
        }

        // Order such that the maps with the most markers are displayed first.
        return mapWaymarks.OrderByDescending(x => x.Value.Count).Select(x => (x.Key, x.Value));
    }

    private static IEnumerable<MapRange> GetMapRanges(TerritoryInfo territory)
    {
        var bg = territory.Bg;
        lock (BgToMapRangeCache)
        {
            if (BgToMapRangeCache.TryGetValue(bg, out var ranges))
                return ranges;

            var rangeList = new List<MapRange>();
            var slashIndex = bg.LastIndexOf('/');
            if (slashIndex == -1) return rangeList;

            var planmapPath = $"bg/{bg[..slashIndex]}/planmap.lgb";
            var planmap = Plugin.DataManager.GetFile<LgbFile>(planmapPath);
            if (planmap != null)
            {
                foreach (var layer in planmap.Layers)
                {
                    foreach (var obj in layer.InstanceObjects.Where(obj => obj.AssetType == LayerEntryType.MapRange))
                    {
                        var mapRange = (MapRangeInstanceObject)obj.Object;
                        if (mapRange.Map > 0)
                            rangeList.Add(new(obj));
                    }
                }
            }
            BgToMapRangeCache.Add(bg, rangeList);
            return rangeList;
        }
    }

    private static ContentType GetContentType(TerritoryType territoryType)
    {
        var contentType = (ContentType)territoryType.ContentFinderCondition.Value.ContentType.RowId;
        if (!Enum.IsDefined(contentType))
            contentType = ContentType.Other;
        return contentType;
    }

    internal static string GetTerritoryName(ushort territoryId)
    {
        return TerritoryIdToInfo.GetValueOrDefault(territoryId).Name;
    }

    internal static Expansion GetExpansion(ushort territoryId)
    {
        return TerritoryIdToInfo.GetValueOrDefault(territoryId).Expansion;
    }
    internal static ExpansionInfo GetExpansionInfo(Expansion expansion)
    {
        return ExpansionInfos[expansion];
    }
    internal static uint GetExpansionIcon(ushort territoryId)
    {
        return GetExpansionInfo(GetExpansion(territoryId)).icon;
    }
    internal static ContentTypeInfo GetContentTypeInfo(ContentType contentType)
    {
        return ContentTypeInfos[contentType];
    }
    internal static uint GetContentTypeIcon(ushort territoryId)
    {
        return GetContentTypeInfo(GetContentType(territoryId)).icon;
    }

    internal static ContentType GetContentType(ushort territoryId)
    {
        return TerritoryIdToInfo.GetValueOrDefault(territoryId).ContentType;
    }

    internal static ushort GetContentId(ushort territoryId)
    {
        return TerritoryIdToInfo.GetValueOrDefault(territoryId).ContentId;
    }

    internal static IEnumerable<ushort> GetAlternativeIds(ushort territoryId)
    {
        if (Plugin.Config.CombineEquivalentDutyPresets && AlternativeTerritoryIds.Contains(territoryId))
        {
            return AlternativeTerritoryIds[territoryId];
        }
        return [territoryId];
    }

    internal static ushort TerritoryIdForContentId(ushort contentId)
    {
        var kv = TerritoryIdToInfo.LastOrDefault(x => x.Value.ContentId == contentId);
        return kv.Key;
    }

    internal static bool IsValid(ushort territoryId)
    {
        return TerritoryIdToInfo.ContainsKey(territoryId);
    }

    internal static bool GetCanUseNativePresets(ushort territoryId)
    {
        if (TerritoryIdToInfo.TryGetValue(territoryId, out var value))
            return value.ArePresetsSupported;
        return false;
    }

    internal static TerritoryInfo GetInfo(ushort territoryId)
    {
        return TerritoryIdToInfo.GetValueOrDefault(territoryId);
    }
}
