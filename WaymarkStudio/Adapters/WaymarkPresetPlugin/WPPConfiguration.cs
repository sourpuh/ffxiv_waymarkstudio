using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;
using System;
using System.IO;

namespace WaymarkStudio.Adapters.WaymarkPresetPlugin;

[Serializable]
public class WPPConfiguration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public WPPWaymarkPresetLibrary PresetLibrary = new();

    public static WPPConfiguration? Load(IDalamudPluginInterface i)
    {
        var parent = Directory.GetParent(i.GetPluginConfigDirectory());
        FileInfo fileInfo = new(Path.Combine(parent.FullName, "WaymarkPresetPlugin.json"));
        if (fileInfo is not { Exists: true } path)
            return null;

        var content = File.ReadAllText(path.FullName);
        return JsonConvert.DeserializeObject<WPPConfiguration>(content);
    }
}
