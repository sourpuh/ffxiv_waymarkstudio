using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.Interop;
using System;
using System.Collections.Generic;

namespace WaymarkStudio;

/**
 * This should handle storage management for plugin and native presets.
 */
internal class PresetStorage
{
    const uint MaxEntries = 30;

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

    public IEnumerable<(int, FieldMarkerPreset)> NativePresets(ushort contentFinderIdFilter = 0)
    {
        for (int i = 0; i < MaxEntries; i++)
        {
            var nativePreset = GetNativePreset((uint)i);
            if (contentFinderIdFilter == 0 || nativePreset.ContentFinderConditionId == contentFinderIdFilter)
                yield return (i, nativePreset);
        }
    }

    public unsafe bool SetNativePreset(int slotNum, FieldMarkerPreset preset)
    {
        if (slotNum >= MaxEntries)
            return false;

        Plugin.Log.Debug($"Attempting to write slot {slotNum} with data:\r\n{preset}");

        var pointer = FieldMarkerModule.Instance()->Presets.GetPointer(slotNum);
        *pointer = preset;
        return true;
    }

    public IEnumerable<(int, WaymarkPreset)> SavedPresets(ushort territoryId = 0)
    {
        var presets = Plugin.Config.SavedPresets;
        for (int i = 0; i < presets.Count; i++)
        {
            var preset = presets[i];
            if (territoryId == 0 || preset.TerritoryId == territoryId)
                yield return (i, preset);
        }
    }

    public void DeleteSavedPreset(int index)
    {
        Plugin.Config.SavedPresets.RemoveAt(index);
        Plugin.Config.Save();
    }
}
