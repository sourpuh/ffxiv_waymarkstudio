using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace WaymarkStudio.Compat.WaymarkPresetPlugin;

[JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
public class WPPWaymarkPreset
{
    public string Name { get; set; } = "Unknown";
    public ushort MapID;
    public DateTimeOffset Time { get; set; } = new(DateTimeOffset.Now.UtcDateTime);
    public WPPWaymark A { get; set; } = new();
    public WPPWaymark B { get; set; } = new();
    public WPPWaymark C { get; set; } = new();
    public WPPWaymark D { get; set; } = new();
    public WPPWaymark One { get; set; } = new();
    public WPPWaymark Two { get; set; } = new();
    public WPPWaymark Three { get; set; } = new();
    public WPPWaymark Four { get; set; } = new();

    public WaymarkPreset ToPreset()
    {
        Dictionary<Waymark, Vector3> markerPositions = new();
        AddWaymarkPosition(markerPositions, A);
        AddWaymarkPosition(markerPositions, B);
        AddWaymarkPosition(markerPositions, C);
        AddWaymarkPosition(markerPositions, D);
        AddWaymarkPosition(markerPositions, One);
        AddWaymarkPosition(markerPositions, Two);
        AddWaymarkPosition(markerPositions, Three);
        AddWaymarkPosition(markerPositions, Four);

        var territoryId = TerritorySheet.TerritoryIdForContentId(MapID);
        WaymarkPreset preset = new(Name, territoryId, markerPositions, Time);
        return preset;
    }

    private static void AddWaymarkPosition(Dictionary<Waymark, Vector3> markerPositions, WPPWaymark waymark)
    {
        if (waymark.Active)
            markerPositions.Add((Waymark)waymark.ID, waymark.Position);
    }
}
