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
        if (preset.PendingHeightAdjustment.IsAnySet())
            throw new ArgumentException("Cannot share a preset with pending height adjustment.");
        (var bytes, var checksum) = Serialize(preset);

        // wms1 format is: `wms1.{b64 preset data}.{b64 crc32 checksum}`
        return $"{Presetb64PrefixV1}.{Base64UrlEncoder.Encode(bytes)}.{Base64UrlEncoder.Encode(checksum)}";
    }

    private static (byte[] bytes, byte[] checksum) Serialize(WaymarkPreset preset)
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
        // There are currently 2 binary formats used for encoding waymark positions:
        // 1. XZ Offset - if all waymarks share the same height, the center of the waymarks is first written as (X,Y,Z) and then each waymark is written as (X,Z)
        // 2. Default - All waymarks are written as (X,Y,Z)
        // Both formats first write an 8 bit mask describing which waymarks are active
        if (useXZOffset)
            SerializeXZOffset(writer, preset, bb.Center.Round());
        else
            SerializeDefault(writer, preset);
        var bytes = memoryStream.ToArray();
        var checksum = Crc32.Hash(bytes);
            
        return (bytes, checksum);
    }

    private static void SerializeDefault(BinaryWriter writer, WaymarkPreset preset)
    {
        writer.Write7BitEncodedInt(FormatDefault);
        writer.Write(preset.TerritoryId);
        writer.Write(preset.PlacedMask);
        foreach (Waymark w in Enum.GetValues<Waymark>())
            if (preset.MarkerPositions.TryGetValue(w, out var position))
                writer.Write(position);
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
