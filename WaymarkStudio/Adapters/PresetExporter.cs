namespace WaymarkStudio.Adapters.WaymarkStudio;
internal static class PresetExporter
{
    internal const string Host = "sourpuh.github.io/waymarkstudio";
    internal const string PresetQueryParam = "preset";

    internal static string Export(WaymarkPreset preset)
    {
        return $"https://{Host}?{PresetQueryParam}={Wms1Exporter.Export(preset)}";
    }
}
