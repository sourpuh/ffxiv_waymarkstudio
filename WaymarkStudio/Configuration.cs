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

    public bool EnableVfxTesting = false;

    public bool SnapXZToGrid = true;
    public bool PlaceRealIfPossible = true;
    public bool CombineEquivalentDutyPresets = true;
    public bool ReplaceNativeUi = true;
    public bool ClearNativeWhenPlacing = false;
    public bool DisableWorldPresetSafetyChecks = false;
    public Dictionary<string, LibraryConfig> LibraryConfiguration = new();

    public class LibraryConfig
    {
        public bool Visible = true;
    }

    public bool IsLibraryVisible(string library)
    {
        return LibraryConfiguration.GetValueOrDefault(library)?.Visible ?? true;
    }

    public void SetLibraryVisibilty(string library, bool visible)
    {
        if (!LibraryConfiguration.TryGetValue(library, out var value))
        {
            value = new();
            LibraryConfiguration.Add(library, value);
        }
        value.Visible = visible;
    }

    public bool NotificationErrorChat = false;
    public bool NotificationErrorDalamud = true;
    public bool NotificationErrorToast = false;

    public bool NotificationSuccessChat = true;
    public bool NotificationSuccessDalamud = false;

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
