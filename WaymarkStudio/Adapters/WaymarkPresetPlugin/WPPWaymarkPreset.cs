using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace WaymarkStudio.Adapters.WaymarkPresetPlugin;

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
        AddWaymarkPosition(markerPositions, Waymark.A, A);
        AddWaymarkPosition(markerPositions, Waymark.B, B);
        AddWaymarkPosition(markerPositions, Waymark.C, C);
        AddWaymarkPosition(markerPositions, Waymark.D, D);
        AddWaymarkPosition(markerPositions, Waymark.One, One);
        AddWaymarkPosition(markerPositions, Waymark.Two, Two);
        AddWaymarkPosition(markerPositions, Waymark.Three, Three);
        AddWaymarkPosition(markerPositions, Waymark.Four, Four);

        var territoryId = TerritorySheet.TerritoryIdForContentId(MapID);
        WaymarkPreset preset = new(Name, territoryId, markerPositions, Time);
        return preset;
    }

    private static void AddWaymarkPosition(Dictionary<Waymark, Vector3> markerPositions, Waymark waymark, WPPWaymark wppWaymark)
    {
        if (wppWaymark.Active)
            markerPositions.Add(waymark, wppWaymark.Position);
    }
}
