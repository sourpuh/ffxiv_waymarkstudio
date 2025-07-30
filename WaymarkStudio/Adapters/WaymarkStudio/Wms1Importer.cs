using Microsoft.IdentityModel.Tokens;
using System;
using System.IO;
using System.IO.Hashing;
using System.Numerics;

namespace WaymarkStudio.Adapters.WaymarkStudio;
internal static class Wms1Importer
{
    const string Presetb64PrefixV1 = "wms1";

    internal static bool IsTextImportable(string text)
    {
        return text.StartsWith(Presetb64PrefixV1);
    }

    internal static WaymarkPreset Import(string text)
    {
        return Deserialize(Base64UrlEncoder.DecodeBytes(text.Substring(Presetb64PrefixV1.Length)));
    }

    private static WaymarkPreset Deserialize(byte[] b)
    {
        WaymarkPreset preset = new();
        using var memoryStream = new MemoryStream(b);
        using var reader = new BinaryReader(memoryStream);
            
        var format = reader.Read7BitEncodedInt();
        if (format == Wms1Exporter.FormatDefault)
            DeserializeDefault(reader, preset);
        else if (format == Wms1Exporter.FormatCompressXZOffset)
            DeserializeXZOffset(reader, preset);
        else
            throw new ArgumentException($"Unsupported preset format '{format}'. Update the plugin and try again.");
        var dataLength = memoryStream.Position;
        var expectedChecksum = reader.ReadUInt32();
        memoryStream.Position = 0;
        memoryStream.SetLength(dataLength);
        var computedChecksum = Crc32.HashToUInt32(memoryStream.ToArray());
        if (computedChecksum != expectedChecksum)
        {
            throw new ArgumentException("Corrupt preset: checksum does not match");
        }
        return preset;
    }

    private static void DeserializeDefault(BinaryReader reader, WaymarkPreset preset)
    {
        preset.TerritoryId = reader.ReadUInt16();
        WaymarkMask active = reader.ReadByte();
        foreach (Waymark w in Enum.GetValues<Waymark>())
            if (active.IsSet(w))
                preset.MarkerPositions.Add(w, reader.ReadVector3());
        preset.Name = reader.ReadString();
    }

    private static void DeserializeXZOffset(BinaryReader reader, WaymarkPreset preset)
    {
        preset.TerritoryId = reader.ReadUInt16();
        WaymarkMask active = reader.ReadByte();
        Vector3 center = reader.ReadVector3();
        foreach (Waymark w in Enum.GetValues<Waymark>())
        {
            if (active.IsSet(w))
            {
                var offset = reader.ReadXZVector2();
                var position = (center + offset).Round();
                preset.MarkerPositions.Add(w, position);
            }
        }
        preset.Name = reader.ReadString();
    }
}
