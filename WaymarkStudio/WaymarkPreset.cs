using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace WaymarkStudio;

[Serializable]
public class WaymarkPreset
{
    public string Name;
    public ushort TerritoryId;
    public DateTimeOffset Time { get; set; } = new(DateTimeOffset.Now.UtcDateTime);

    public WaymarkMask PlacedMask {
        get
        {
            WaymarkMask mask = new();
            foreach (var item in MarkerPositions.Keys)
                mask.Set(item, true);
            return mask;
        }
    }
    public IDictionary<Waymark, Vector3> MarkerPositions;
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public WaymarkMask PendingHeightAdjustment;

    [JsonConstructor]
    public WaymarkPreset(string name = "", ushort territoryId = 0, IDictionary<Waymark, Vector3>? markerPositions = null, DateTimeOffset? time = null, WaymarkMask pendingHeightAdjustment = default)
    {
        Name = name;
        TerritoryId = territoryId;
        MarkerPositions = markerPositions ?? new Dictionary<Waymark, Vector3>();
        Time = time ?? new(DateTimeOffset.Now.UtcDateTime);
        PendingHeightAdjustment = pendingHeightAdjustment;
    }

    public float DistanceToNearestNonAdjustedMarker(Vector3 position)
    {
        return MarkerPositions.Where(x => PendingHeightAdjustment.IsSet(x.Key)).Select(x => (position.XZ() - x.Value.XZ()).Length()).Min();
    }

    public virtual FontAwesomeIcon GetIcon()
    {
        return PendingHeightAdjustment.IsAnySet() ? FontAwesomeIcon.Map : FontAwesomeIcon.MapMarkedAlt;
    }

    public override string? ToString()
    {
        return Name;
    }

    // TODO Keeping this?
    public FieldMarkerPreset ToGamePreset()
    {
        FieldMarkerPreset preset = new();

        foreach (Waymark w in Enum.GetValues<Waymark>())
        {
            var index = (int)w;
            bool active = MarkerPositions.ContainsKey(w);
            preset.SetMarkerActive(index, active);
            preset.Markers[index] = active ? MarkerPositions[w].ToGamePresetPoint() : default;
        }

        preset.ContentFinderConditionId = TerritorySheet.GetContentId(TerritoryId);
        preset.Timestamp = (int)Time.ToUnixTimeSeconds();

        return preset;
    }

    internal bool IsCompatibleTerritory(ushort territoryId)
    {
        return TerritoryId == territoryId || Plugin.Config.CombineEquivalentDutyPresets && TerritoryId == TerritorySheet.GetAlternativeId(territoryId);
    }

    internal void MarkPendingHeightAdjustment()
    {
        foreach (Waymark w in MarkerPositions.Keys)
            PendingHeightAdjustment.Set(w, true);
    }

    public bool Equals(WaymarkPreset other)
    {
        return other.Name == Name
            && other.Time == Time
            && IsEquivalent(other);
    }

    public bool IsEquivalent(WaymarkPreset other)
    {
        return
            other.IsCompatibleTerritory(TerritoryId)
            && other.PendingHeightAdjustment == PendingHeightAdjustment
            && other.MarkerPositions.DeepEquals(MarkerPositions);
    }

    public WaymarkPreset Clone()
    {
        return new(Name, TerritoryId, MarkerPositions.Clone(), Time, PendingHeightAdjustment);
    }
}
