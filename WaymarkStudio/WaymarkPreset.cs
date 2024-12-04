using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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

    internal unsafe byte[] Serialize()
    {
        using (var memoryStream = new MemoryStream())
        {
            using (var writer = new BinaryWriter(memoryStream))
            {
                writer.Write(TerritoryId);
                writer.Write(ContentFinderConditionId);
                // write empty bitmask to advance the offset
                byte active = 0;
                writer.Write(active);
                foreach (Waymark w in Enum.GetValues<Waymark>())
                {
                    if (MarkerPositions.ContainsKey(w))
                    {
                        var index = (int)w;
                        active |= (byte)(1 << index);

                        var position = MarkerPositions[w].ToGamePresetPoint();
                        writer.Write7BitEncodedIntSigned(position.X);
                        writer.Write7BitEncodedIntSigned(position.Y);
                        writer.Write7BitEncodedIntSigned(position.Z);
                    }
                }
                writer.Write(Name);
                // write filled bitmask
                writer.Seek(4, SeekOrigin.Begin);
                writer.Write(active);
            }
            return memoryStream.ToArray();
        }
    }

    internal static WaymarkPreset Deserialize(byte[] b)
    {
        WaymarkPreset preset = new();
        using (var memoryStream = new MemoryStream(b))
        {
            using (var reader = new BinaryReader(memoryStream))
            {
                preset.TerritoryId = reader.ReadUInt16();
                preset.ContentFinderConditionId = reader.ReadUInt16();
                byte active = reader.ReadByte();
                foreach (Waymark w in Enum.GetValues<Waymark>())
                {
                    if (Extensions.IsBitSet(active, (int)w))
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
}
