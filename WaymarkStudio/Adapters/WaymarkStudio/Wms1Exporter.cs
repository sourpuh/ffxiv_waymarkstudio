using Microsoft.IdentityModel.Tokens;
using System;
using System.IO;
using System.IO.Hashing;
using System.Numerics;
using WaymarkStudio.Maps;

namespace WaymarkStudio.Adapters.WaymarkStudio;
internal static class Wms1Exporter
{
    internal const string Presetb64PrefixV1 = "wms1";
    internal const int ExpectedMaxMemorySize = 192;
    internal const int FormatDefault = 0;
    internal const int FormatCompressXZOffset = 1;

    internal static string Export(WaymarkPreset preset)
    {
        return Presetb64PrefixV1 + Base64UrlEncoder.Encode(Serialize(preset));
    }

    private static byte[] Serialize(WaymarkPreset preset)
    {
        AABB bb = AABB.BoundingPoints(preset.MarkerPositions.Values);
        var useXZOffset = bb.Max.Y == bb.Min.Y;

        using var memoryStream = new MemoryStream(ExpectedMaxMemorySize);
        using var writer = new BinaryWriter(memoryStream);

        // Standard format for all wms1 presets is:
        // 1. 7 bit encoded int format
        // 2. Territory ID
        // 3. Waymark positions
        // 4. Name
        // 5. crc32 checksum
        if (useXZOffset)
            SerializeXZOffset(writer, preset, bb.Center.Round());
        else
            SerializeDefault(writer, preset);
        var bytes = memoryStream.ToArray();
        writer.Write(Crc32.HashToUInt32(bytes));
            
        return memoryStream.ToArray();
    }

    private static void SerializeDefault(BinaryWriter writer, WaymarkPreset preset)
    {
        writer.Write7BitEncodedInt(FormatDefault);
        writer.Write(preset.TerritoryId);
        writer.Write(preset.PlacedMask);
        foreach (Waymark w in Enum.GetValues<Waymark>())
            if (preset.MarkerPositions.ContainsKey(w))
                writer.Write(preset.MarkerPositions[w]);
        writer.Write(preset.Name);
    }

    private static void SerializeXZOffset(BinaryWriter writer, WaymarkPreset preset, Vector3 center)
    {
        writer.Write7BitEncodedInt(FormatCompressXZOffset);
        writer.Write(preset.TerritoryId);
        writer.Write(preset.PlacedMask);
        writer.Write(center);
        foreach (Waymark w in Enum.GetValues<Waymark>())
        {
            if (preset.MarkerPositions.TryGetValue(w, out var position))
            {
                var offset = (position - center).Round();
                writer.Write(offset.XZ());
            }
        }
        writer.Write(preset.Name);
    }
}
