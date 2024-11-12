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
    const uint Pages = 6;
    const uint EntriesPerPage = 5;
    const uint MaxEntries = Pages * EntriesPerPage;

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
    public unsafe FieldMarkerPreset GetNativePreset(uint page, uint pageIndex)
    {
        return GetNativePreset(page * EntriesPerPage + pageIndex);
    }

    public IEnumerable<(uint, FieldMarkerPreset)> NativePresets(ushort contentFinderIdFilter = 0)
    {
        for (uint i = 0; i < MaxEntries; i++)
        {
            var nativePreset = GetNativePreset(i);
            if (contentFinderIdFilter == 0 || nativePreset.ContentFinderConditionId == contentFinderIdFilter)
                yield return (i, GetNativePreset(i));
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
}
