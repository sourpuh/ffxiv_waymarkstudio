using System;
using System.IO;

namespace WaymarkStudio.Adapters.WaymarkStudio;
internal static class Wms0Exporter
{
    internal const string presetb64PrefixV0 = "wms0";
    internal const int expectedMaxMemorySize = 192;

    internal static string Export(WaymarkPreset preset)
    {
        return presetb64PrefixV0 + Convert.ToBase64String(Serialize(preset));
    }

    private static byte[] Serialize(WaymarkPreset preset)
    {
        using var memoryStream = new MemoryStream(expectedMaxMemorySize);
        using var writer = new BinaryWriter(memoryStream);

        writer.Write(preset.TerritoryId);
        writer.Write((ushort)0);
        writer.Write(preset.PlacedMask);
        foreach (Waymark w in Enum.GetValues<Waymark>())
            if (preset.MarkerPositions.ContainsKey(w))
                writer.Write(preset.MarkerPositions[w]);
        writer.Write(preset.Name);
    
        return memoryStream.ToArray();
    }
}
