using System;

namespace WaymarkStudio.Adapters.WaymarkStudio;
internal static class PresetExporter
{
    internal const string Host = "sourpuh.github.io/waymarkstudio";
    internal const string PresetQueryParam = "preset";

    internal static string Export(WaymarkPreset preset)
    {
        if (preset.PendingHeightAdjustment.IsAnySet())
            throw new ArgumentException("Cannot export a preset with pending height adjustment.");
        return $"https://{Host}?{PresetQueryParam}={Wms1Exporter.Export(preset)}";
    }
}
