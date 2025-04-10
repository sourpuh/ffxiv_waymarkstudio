using Dalamud.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using WaymarkStudio.Triggers;

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

    [JsonIgnore]
    public List<(CircleTrigger, WaymarkPreset?)> Triggers = [];
    [JsonProperty]
#pragma warning disable IDE1006 // Naming Styles
    private List<(CircleTrigger, int)> TriggerPresetIndices = [];
#pragma warning restore IDE1006 // Naming Styles

    public void Save()
    {
        Plugin.Interface.SavePluginConfig(this);
    }

    [OnSerializing]
    internal void OnSerializingMethod(StreamingContext context)
    {
        TriggerPresetIndices = Triggers.Select(e => (e.Item1, SavedPresets.IndexOf(e.Item2))).ToList();
    }

    [OnDeserialized]
    internal void OnDeserializedMethod(StreamingContext context)
    {
        Triggers = TriggerPresetIndices.Select(e => (e.Item1, e.Item2 == -1 ? null : SavedPresets[e.Item2])).ToList();
    }

    [JsonIgnore]
    public int WaymarkPlacementFrequency => DisableWorldPresetSafetyChecks ? 1 : 60;
}
