using Microsoft.IdentityModel.Tokens;
using System;
using System.IO;
using System.IO.Hashing;
using System.Numerics;

namespace WaymarkStudio.Adapters.WaymarkStudio;
internal static class Wms1Importer
{
    const string Presetb64PrefixV1 = "wms1.";

    internal static bool IsTextImportable(string text)
    {
        return text.StartsWith(Presetb64PrefixV1);
    }

    internal static WaymarkPreset Import(string text)
    {
        if(!IsTextImportable(text))
            throw new ArgumentException($"Unable to import preset: missing wms1 prefix");
        string[] parts = text.Split('.');
        if (parts.Length != 3)
            throw new ArgumentException($"Unable to import preset: unexpected preset part count {parts.Length}");
        (var preset, var computedChecksum) = Deserialize(Base64UrlEncoder.DecodeBytes(parts[1]));
        var expectedChecksum = Base64UrlEncoder.DecodeBytes(parts[2]);
        if (!expectedChecksum.SequenceEqual(computedChecksum))
            throw new ArgumentException("Unable to import preset: corrupted; checksum does not match");
        return preset;
    }

    private static (WaymarkPreset preset, byte[] computedChecksum) Deserialize(byte[] b)
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
            throw new ArgumentException($"Unable to deserialize preset: unsupported preset format '{format}'. Update the plugin and try again.");
        var dataLength = memoryStream.Position;
        memoryStream.Position = 0;
        memoryStream.SetLength(dataLength);
        var computedChecksum = Crc32.Hash(memoryStream.ToArray());
        return (preset, computedChecksum);
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
