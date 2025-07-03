using Lumina.Data.Files;
using Lumina.Data.Parsing.Layer;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using WaymarkStudio.Maps;

namespace WaymarkStudio;

using static Lumina.Data.Parsing.Layer.LayerCommon;
using ContentTypeInfo = (uint icon, string name);
using ExpansionInfo = (uint icon, string name);
using LuminaContentType = Lumina.Excel.Sheets.ContentType;
using TerritoryType = Lumina.Excel.Sheets.TerritoryType;
using ExVersion = Lumina.Excel.Sheets.ExVersion;
using ContentFinderCondition = Lumina.Excel.Sheets.ContentFinderCondition;

internal static class TerritorySheet
{
    private static Dictionary<Expansion, ExpansionInfo> ExpansionInfos;
    private static Dictionary<ContentType, ContentTypeInfo> ContentTypeInfos;
    private static Dictionary<ushort, ushort> EquivalentTerritoryIds = new()
    {
        [1075] = 1076, // ASS
        [1155] = 1156, // AMR
        [1179] = 1180, // AAI
        // [1153] = 1154, // P12 for testing
    };

    struct TerritoryInfo
    {
        internal string name;
        internal ushort contentId;
        internal Expansion expansion;
        internal ContentType contentType;
        internal string bg;
        internal uint mapId;
    }
    private static Dictionary<ushort, TerritoryInfo> TerritoryIdToInfo;
    private static Dictionary<string, IEnumerable<MapRange>> BgToMapRangeCache = new();

    static TerritorySheet()
    {
        foreach (var kvp in EquivalentTerritoryIds.ToList())
            EquivalentTerritoryIds.Add(kvp.Value, kvp.Key);

        TerritoryIdToInfo = Plugin.DataManager.GetExcelSheet<TerritoryType>()
            .ToDictionary(x => (ushort)x.RowId,
            x => new TerritoryInfo()
            {
                name = x.GetName(),
                contentId = (ushort)x.GetContentId(),
                expansion = (Expansion)x.ExVersion.RowId,
                contentType = GetContentType(x),
                bg = x.Bg.ExtractText(),
                mapId = x.Map.RowId,
            });

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

    public static IEnumerable<(uint, ISet<Waymark>)> GetPresetMapIds(WaymarkPreset preset)
    {
        var territory = TerritoryIdToInfo[preset.TerritoryId];
        var ranges = GetMapRanges(territory);

        Dictionary<uint, ISet<Waymark>> mapWaymarks = [];
        foreach(var range in ranges)
        {
            var waymarks = preset.MarkerPositions.Where(x => range.Contains(x.Value)).Select(x => x.Key).ToHashSet();
            if (waymarks.Any())
                if (mapWaymarks.TryGetValue(range.MapId, out var waymarkSet))
                    waymarkSet.UnionWith(waymarks);
                else
                    mapWaymarks[range.MapId] = waymarks;
        }

        // Purge the default map if another map exists with the same set of waymarks.
        // This is necessary because M8S has 2 similar maps for the main ring that show the same set.
        var defaultMapId = territory.mapId;
        if (mapWaymarks.TryGetValue(defaultMapId, out var defaultMapWaymarks))
        {
            foreach ((var mapId, var waymarks) in mapWaymarks)
            {
                if (mapId != defaultMapId && waymarks.SequenceEqual(defaultMapWaymarks))
                {
                    mapWaymarks.Remove(defaultMapId);
                    break;
                }
            }
        }

        var mappedWaymarks = mapWaymarks.Select(x => x.Value).SelectMany(x => x).ToHashSet();
        var unmappedWaymarks = preset.MarkerPositions.Keys.Where(x => !mappedWaymarks.Contains(x));

        // If there was no MapRange containing the waymarks, return the default map.
        if (unmappedWaymarks.Any())
        {
            mapWaymarks[defaultMapId] = preset.MarkerPositions.Keys.ToHashSet();
        }

        // Order such that the maps with the most markers are displayed first.
        return mapWaymarks.OrderByDescending(x => x.Value.Count).Select(x => (x.Key, x.Value));
    }

    private static IEnumerable<MapRange> GetMapRanges(TerritoryInfo territory)
    {
        var bg = territory.bg;
        IEnumerable<MapRange>? ranges;
        if (!BgToMapRangeCache.TryGetValue(bg, out ranges))
        {
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
            ranges = rangeList;
        }

        return ranges;
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
        return TerritoryIdToInfo[territoryId].name;
    }

    internal static Expansion GetExpansion(ushort territoryId)
    {
        return TerritoryIdToInfo[territoryId].expansion;
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
        return TerritoryIdToInfo[territoryId].contentType;
    }

    internal static string GetContentName(ushort contentId)
    {
        if (Plugin.DataManager.GetExcelSheet<ContentFinderCondition>().TryGetRow(contentId, out var content))
            return content.Name.ToString();
        return "";
    }

    internal static ushort GetContentId(ushort territoryId)
    {
        if (TerritoryIdToInfo.TryGetValue(territoryId, out var value))
            return (ushort)value.contentId;
        return 0;
    }

    internal static ushort? GetAlternativeId(ushort territoryId)
    {
        if (Plugin.Config.CombineEquivalentDutyPresets && EquivalentTerritoryIds.TryGetValue(territoryId, out var altTerritoryId))
            return altTerritoryId;
        return null;
    }

    internal static ushort TerritoryIdForContentId(ushort contentId)
    {
        var kv = TerritoryIdToInfo.Where(x => x.Value.contentId == contentId).LastOrDefault();
        return kv.Key;
    }

    internal static bool IsValid(ushort territoryId)
    {
        return TerritoryIdToInfo.ContainsKey(territoryId);
    }
}
