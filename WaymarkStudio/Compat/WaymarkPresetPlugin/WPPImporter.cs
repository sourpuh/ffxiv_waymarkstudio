using Newtonsoft.Json;

namespace WaymarkStudio.Compat.WaymarkPresetPlugin;

public static class WPPImporter
{
    public static WaymarkPreset Import(string presetString)
    {
        var wppPreset = JsonConvert.DeserializeObject<WPPWaymarkPreset>(presetString);
        return wppPreset.ToPreset();
    }
}
