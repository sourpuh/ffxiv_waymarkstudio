using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using WaymarkStudio.Compat.MemoryMarker;
using WaymarkStudio.Compat.WaymarkPresetPlugin;

namespace WaymarkStudio;

/**
 * This should handle storage management for plugin and native presets.
 */
internal class PresetStorage
{
    internal const string WPP = "Waymark Preset Plugin Presets";
    internal const string MM = "Memory Marker Presets";
    internal const string Native = "Native Presets";
    internal const string Community = "Community Presets";

    const uint MaxEntries = 30;
    private TerritoryFilter lastFilter;
    public PresetLibrary Library;
    public PresetLibrary WPPLibrary;
    public PresetLibrary MMLibrary;
    public PresetLibrary NativeLibrary;
    public PresetLibrary CommunityLibrary;

    internal PresetStorage()
    {
        Library = new(ListSavedPresets, () => true);
        WPPLibrary = new(ListWPPPresets, () => Plugin.Config.IsLibraryVisible(WPP));
        MMLibrary = new(ListMMPresets, () => Plugin.Config.IsLibraryVisible(MM));
        NativeLibrary = new(ListNativePresets, () => Plugin.Config.IsLibraryVisible(Native));
        CommunityLibrary = new(ListCommunityPresets, () => Plugin.Config.IsLibraryVisible(Community));
    }

    public int CountPresetsForTerritoryId(uint territoryId)
    {
        int count = 0;
        foreach (var preset in Plugin.Config.SavedPresets)
        {
            if (territoryId == preset.TerritoryId) count++;
        }
        return count;
    }

    public unsafe FieldMarkerPreset GetNativePreset(uint index)
    {
        if (index >= MaxEntries)
        {
            throw new ArgumentOutOfRangeException($"Illegal Index: {index} >= {MaxEntries}");
        }
        return FieldMarkerModule.Instance()->Presets[(int)index];
    }

    public bool ContainsEquivalentPreset(WaymarkPreset preset)
    {
        return Plugin.Config.SavedPresets.Where(x => x.IsEquivalent(preset)).Any();
    }

    public void SavePreset(WaymarkPreset preset)
    {
        Library.InvalidateCache();
        Plugin.Config.SavedPresets.Add(preset);
        SaveConfig();
    }

    public void DeleteSavedPreset(int index)
    {
        Library.InvalidateCache();
        Plugin.Config.SavedPresets.RemoveAt(index);
        SaveConfig();
    }

    public void MovePreset(int sourceIndex, int targetIndex)
    {
        Plugin.Config.SavedPresets.Move(sourceIndex, targetIndex);
        SaveConfig();
    }

    private void SaveConfig()
    {
        Library.InvalidateCache();
        Plugin.Config.Save();
    }

    private IEnumerable<(int, WaymarkPreset)> ListSavedPresets()
    {
        var presets = Plugin.Config.SavedPresets;
        for (int i = 0; i < presets.Count; i++)
        {
            yield return (i, presets[i]);
        }
    }

    private IEnumerable<(int, WaymarkPreset)> ListCommunityPresets()
    {
        if (!GitHubLoader.Presets.IsCompletedSuccessfully)
        {
            return Enumerable.Empty<(int, WaymarkPreset)>();
        }

        int i = 0;
        return GitHubLoader.Presets.Result.Select(x => (i++, x));
    }

    private IEnumerable<(int, WaymarkPreset)> ListWPPPresets()
    {
        var wppConfig = WPPConfiguration.Load(Plugin.Interface);
        if (wppConfig != null)
        {
            int i = 0;
            foreach (var wppPreset in wppConfig.PresetLibrary.Presets)
            {
                yield return (i++, wppPreset.ToPreset());
            }
        }
    }

    private IEnumerable<(int, WaymarkPreset)> ListMMPresets()
    {
        var mmConfig = MMConfiguration.Load(Plugin.Interface);
        if (mmConfig != null)
        {
            int i = 0;
            foreach (var territoryIdToPreset in mmConfig.FieldMarkerData)
            {
                var presets = territoryIdToPreset.Value.MarkerData;
                for (int j = 0; j < presets.Count; j++)
                {
                    var preset = presets[j];
                    if (preset != null
                        && territoryIdToPreset.Key == TerritorySheet.TerritoryIdForContentId(preset.Marker.ContentFinderConditionId))
                    {
                        var name = preset.Name.Length == 0 ? $"Slot {j + 1}" : preset.Name;
                        yield return (i++, preset.Marker.ToPreset(name));
                    }
                }
            }
        }
    }

    private IEnumerable<(int, WaymarkPreset)> ListNativePresets()
    {
        for (int i = 0; i < MaxEntries; i++)
        {
            var nativePreset = GetNativePreset((uint)i);
            if (nativePreset.ActiveMarkers == 0) continue;
            yield return (i, nativePreset.ToPreset($"Slot {i + 1}"));
        }
    }

    // Don't use without testing
    [Obsolete]
    public unsafe bool SetNativePreset(int slotNum, FieldMarkerPreset preset)
    {
        if (slotNum >= MaxEntries)
            return false;

        Plugin.Log.Debug($"Attempting to write slot {slotNum} with data:\r\n{preset}");

        var pointer = FieldMarkerModule.Instance()->Presets.GetPointer(slotNum);
        *pointer = preset;
        return true;
    }

    public unsafe void Update()
    {
        if (FieldMarkerModule.Instance()->GetHasChanges())
        {
            NativeLibrary.InvalidateCache();
            MMLibrary.InvalidateCache();
        }
    }
}
