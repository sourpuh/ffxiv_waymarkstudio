using System;
using System.Web;
using WaymarkStudio.Adapters.WaymarkPresetPlugin;
using WaymarkStudio.Adapters.WaymarkStudio;

namespace WaymarkStudio.Adapters;
internal static class PresetImporter
{
    internal static bool IsTextImportable(string text)
    {
        return text.Contains(PresetExporter.Host) ||
            Wms0Importer.IsTextImportable(text) ||
            Wms1Importer.IsTextImportable(text) ||
            WPPImporter.IsTextImportable(text);
    }

    internal static WaymarkPreset? Import(string text)
    {
        try
        {
            text = ExtractPreset(text);
            if (Wms0Importer.IsTextImportable(text))
                return Wms0Importer.Import(text);
            if (Wms1Importer.IsTextImportable(text))
                return Wms1Importer.Import(text);
            if (WPPImporter.IsTextImportable(text))
                return WPPImporter.Import(text);
            throw new ArgumentException($"Waymark preset import failed. Try updating your plugin and check if your clipboard contains a valid preset and try again.");
        }
        catch (Exception ex)
        {
            Plugin.ReportError(ex);
        }
        return null;
    }

    internal static string ExtractPreset(string text)
    {
        if (text.Contains(PresetExporter.Host))
        {
            var uri = new Uri(text);
            var query = HttpUtility.ParseQueryString(uri.Query);
            var preset = query.Get(PresetExporter.PresetQueryParam);
            if (preset != null)
                text = preset;
        }
        return text;
    }
}
