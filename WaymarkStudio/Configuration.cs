using Dalamud.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace WaymarkStudio;

[Serializable]
public class Configuration : IPluginConfiguration
{
    private static JsonSerializerSettings JsonSerializerSettings = new()
    {
        TypeNameHandling = TypeNameHandling.None,
        Formatting = Formatting.None,
        ObjectCreationHandling = ObjectCreationHandling.Replace,
    };

    public int Version { get; set; } = 0;

    public bool SnapXZToGrid = true;
    public bool PlaceRealIfPossible = false;
    public bool CombineEquivalentDutyPresets = true;
    public bool ReplaceNativeUi = true;
    public bool ClearNativeWhenPlacing = false;
    public bool DisableWorldPresetSafetyChecks = false;
    public List<WaymarkPreset> SavedPresets { get; set; } = [];

    public void Save()
    {
        Plugin.Interface.SavePluginConfig(this);
    }

    [JsonIgnore]
    public int WaymarkPlacementFrequency => DisableWorldPresetSafetyChecks ? 1 : 60;
}
