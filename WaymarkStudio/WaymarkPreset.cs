using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace WaymarkStudio;

[Serializable]
public class WaymarkPreset
{
    internal const string presetb64Prefix = "wms0";

    public string Name;
    public ushort TerritoryId;
    [JsonIgnore]
    [Obsolete]
    // Delete once a new b64 export is defined.
    public ushort ContentFinderConditionId;
    public DateTimeOffset Time { get; set; } = new(DateTimeOffset.Now.UtcDateTime);

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

    internal byte[] Serialize()
    {
        using (var memoryStream = new MemoryStream())
        {
            using (var writer = new BinaryWriter(memoryStream))
            {
                writer.Write(TerritoryId);
                writer.Write((ushort)0);
                // write empty active mask to advance the offset
                WaymarkMask active = default;
                writer.Write(active);
                foreach (Waymark w in Enum.GetValues<Waymark>())
                {
                    if (MarkerPositions.ContainsKey(w))
                    {
                        active.Set(w, true);
                        var position = MarkerPositions[w].ToGamePresetPoint();
                        writer.Write7BitEncodedIntSigned(position.X);
                        writer.Write7BitEncodedIntSigned(position.Y);
                        writer.Write7BitEncodedIntSigned(position.Z);
                    }
                }
                writer.Write(Name);
                // rewind and write real active mask
                writer.Seek(4, SeekOrigin.Begin);
                writer.Write(active);
            }
            return memoryStream.ToArray();
        }
    }

    internal string Export()
    {
        return presetb64Prefix + Convert.ToBase64String(Serialize());
    }

    internal static WaymarkPreset Deserialize(byte[] b)
    {
        WaymarkPreset preset = new();
        using (var memoryStream = new MemoryStream(b))
        {
            using (var reader = new BinaryReader(memoryStream))
            {
                preset.TerritoryId = reader.ReadUInt16();
                _ = reader.ReadUInt16();
                WaymarkMask active = reader.ReadByte();
                foreach (Waymark w in Enum.GetValues<Waymark>())
                {
                    if (active.IsSet(w))
                    {
                        GamePresetPoint position = new()
                        {
                            X = reader.Read7BitEncodedIntSigned(),
                            Y = reader.Read7BitEncodedIntSigned(),
                            Z = reader.Read7BitEncodedIntSigned()
                        };
                        preset.MarkerPositions.Add(w, position.ToWorldPosition());
                    }
                }
                preset.Name = reader.ReadString();
            }
        }
        return preset;
    }

    internal static WaymarkPreset Import(string s)
    {
        return Deserialize(Convert.FromBase64String(s.Substring(presetb64Prefix.Length)));
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
