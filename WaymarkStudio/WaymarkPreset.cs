using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace WaymarkStudio;

[Serializable]
public class WaymarkPreset
{
    public string Name;
    public ushort TerritoryId;
    public ushort ContentFinderConditionId;
    public DateTimeOffset Time { get; set; } = new(DateTimeOffset.Now.UtcDateTime);

    public IDictionary<Waymark, Vector3> MarkerPositions;

    [JsonConstructor]
    public WaymarkPreset(string name = "", ushort territoryId = 0, ushort contentFinderConditionId = 0, IDictionary<Waymark, Vector3>? markerPositions = null, DateTimeOffset? time = null)
    {
        Name = name;
        TerritoryId = territoryId;
        ContentFinderConditionId = contentFinderConditionId;
        MarkerPositions = markerPositions ?? new Dictionary<Waymark, Vector3>();
        Time = time ?? new(DateTimeOffset.Now.UtcDateTime);
    }

    public FieldMarkerPreset ToGamePreset()
    {
        FieldMarkerPreset preset = new();

        foreach (Waymark w in Enum.GetValues<Waymark>())
        {
            int index = (int)w;
            bool active = MarkerPositions.ContainsKey(w);
            preset.SetMarkerActive(index, active);
            preset.Markers[index] = active ? MarkerPositions[w].ToGamePresetPoint() : default;
        }

        preset.ContentFinderConditionId = ContentFinderConditionId;
        preset.Timestamp = (int)Time.ToUnixTimeSeconds();

        return preset;
    }

    internal static ushort TerritoryIdForContendId(ushort contentId)
    {
        var contentSheet = Plugin.DataManager.GetExcelSheet<ContentFinderCondition>();
        var row = contentSheet.GetRow(contentId);
        return (ushort)row.TerritoryType.Value.RowId;
    }
}
