using Dalamud.Configuration;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace WaymarkStudio.Adapters.MemoryMarker;

[Serializable]
internal class MMConfiguration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public Dictionary<uint, MMZoneMarkerData> FieldMarkerData = [];

    public class MMZoneMarkerData
    {
        public List<NamedMarker> MarkerData { get; init; } = [];
    }

    public class NamedMarker
    {
        public string Name = string.Empty;
        public MMFieldMarkerPreset Marker { get; set; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0, Size = 0x68)]
    public struct MMFieldMarkerPreset
    {
        public GamePresetPoint A;
        public GamePresetPoint B;
        public GamePresetPoint C;
        public GamePresetPoint D;
        public GamePresetPoint One;
        public GamePresetPoint Two;
        public GamePresetPoint Three;
        public GamePresetPoint Four;
        public byte ActiveMarkers;
        public ushort ContentFinderConditionId;
        public int Timestamp;

        public WaymarkPreset ToPreset(string name = "")
        {
            WaymarkPreset p = new(name, TerritorySheet.TerritoryIdForContentId(ContentFinderConditionId), null, DateTimeOffset.FromUnixTimeSeconds(Timestamp));
            WaymarkMask activeMask = ActiveMarkers;
            if (activeMask.IsSet(Waymark.A))
                p.MarkerPositions.Add(Waymark.A, A.ToWorldPosition());
            if (activeMask.IsSet(Waymark.B))
                p.MarkerPositions.Add(Waymark.B, B.ToWorldPosition());
            if (activeMask.IsSet(Waymark.C))
                p.MarkerPositions.Add(Waymark.C, C.ToWorldPosition());
            if (activeMask.IsSet(Waymark.D))
                p.MarkerPositions.Add(Waymark.D, D.ToWorldPosition());
            if (activeMask.IsSet(Waymark.One))
                p.MarkerPositions.Add(Waymark.One, One.ToWorldPosition());
            if (activeMask.IsSet(Waymark.Two))
                p.MarkerPositions.Add(Waymark.Two, Two.ToWorldPosition());
            if (activeMask.IsSet(Waymark.Three))
                p.MarkerPositions.Add(Waymark.Three, Three.ToWorldPosition());
            if (activeMask.IsSet(Waymark.Four))
                p.MarkerPositions.Add(Waymark.Four, Four.ToWorldPosition());
            return p;
        }
    }

    public static MMConfiguration? Load(IDalamudPluginInterface i)
    {
        var parent = Directory.GetParent(i.GetPluginConfigDirectory());
        FileInfo fileInfo = new(Path.Combine(parent.FullName, "MemoryMarker.json"));
        if (fileInfo is not { Exists: true } path)
            return null;

        var content = File.ReadAllText(path.FullName);
        return JsonConvert.DeserializeObject<MMConfiguration>(content);
    }
}
