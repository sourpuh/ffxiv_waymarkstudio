using System;
using System.IO;

namespace WaymarkStudio.Adapters.WaymarkStudio;
internal static class Wms0Importer
{
    internal const string presetb64PrefixV0 = "wms0";

    internal static bool IsTextImportable(string text)
    {
        return text.StartsWith(presetb64PrefixV0);
    }

    internal static WaymarkPreset Import(string text)
    {
        return Deserialize(Convert.FromBase64String(text.Substring(presetb64PrefixV0.Length)));
    }

    private static WaymarkPreset Deserialize(byte[] b)
    {
        WaymarkPreset preset = new();
        using var memoryStream = new MemoryStream(b);
        using var reader = new BinaryReader(memoryStream);
            
        preset.TerritoryId = reader.ReadUInt16();
        _ = reader.ReadUInt16();
        WaymarkMask active = reader.ReadByte();
        foreach (Waymark w in Enum.GetValues<Waymark>())
            if (active.IsSet(w))
                preset.MarkerPositions.Add(w, reader.ReadVector3());
        preset.Name = reader.ReadString();
        
        return preset;
    }
}
