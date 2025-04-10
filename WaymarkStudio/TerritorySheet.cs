using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WaymarkStudio;

using ContentTypeInfo = (uint icon, string name);
using ExpansionInfo = (uint icon, string name);
using LuminaContentType = Lumina.Excel.Sheets.ContentType;

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
        internal uint contentId;
        internal Expansion expansion;
        internal ContentType contentType;
    }
    private static Dictionary<uint, TerritoryInfo> TerritoryIdToInfo;

    static TerritorySheet()
    {
        foreach (var kvp in EquivalentTerritoryIds.ToList())
            EquivalentTerritoryIds.Add(kvp.Value, kvp.Key);

        TerritoryIdToInfo = Plugin.DataManager.GetExcelSheet<TerritoryType>()
            .ToDictionary(x => x.RowId,
            x => new TerritoryInfo()
            {
                name = x.GetName(),
                contentId = x.GetContentId(),
                expansion = (Expansion)x.ExVersion.RowId,
                contentType = GetContentType(x),
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
}
