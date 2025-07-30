using Newtonsoft.Json;

namespace WaymarkStudio.Adapters.WaymarkPresetPlugin;

public static class WPPImporter
{
    public static bool IsTextImportable(string text)
    {
        return text.StartsWith("{");
    }

    public static WaymarkPreset Import(string presetString)
    {
        var wppPreset = JsonConvert.DeserializeObject<WPPWaymarkPreset>(presetString);
        return wppPreset.ToPreset();
    }
}
