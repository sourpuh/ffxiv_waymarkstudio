using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.Interop;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace WaymarkStudio;

using PresetLibrary = ImmutableSortedDictionary<ushort, List<(int, WaymarkPreset)>>;

/**
 * This should handle storage management for plugin and native presets.
 */
internal class PresetStorage
{
    const uint MaxEntries = 30;

    private Dictionary<uint, uint> equivalentTerritoryIds = new()
    {
        [1075] = 1076, // ASS
        [1155] = 1156, // AMR
        [1179] = 1180, // AAI
        // [1153] = 1154, // P12 for testing
    };

    internal PresetStorage()
    {
        foreach (var kvp in equivalentTerritoryIds.ToList())
            equivalentTerritoryIds.Add(kvp.Value, kvp.Key);
    }

    public PresetLibrary GetPresetLibrary(TerritoryFilter filter)
    {
        return ListSavedPresets()
            .Where(preset => !filter.IsTerritoryFiltered(preset.Item2.TerritoryId))
            .GroupBy(preset => preset.Item2.TerritoryId, v => v)
            .ToImmutableSortedDictionary(g => g.Key, g => g.ToList());
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

    public IEnumerable<(int, FieldMarkerPreset)> ListNativePresets(ushort territoryId = 0)
    {
        uint contentId = TerritorySheet.GetContentId(territoryId);
        uint altContentId = 0;
        if (Plugin.Config.CombineEquivalentDutyPresets && equivalentTerritoryIds.TryGetValue(territoryId, out var altTerritoryId))
            equivalentTerritoryIds.TryGetValue(altTerritoryId, out altContentId);

        for (int i = 0; i < MaxEntries; i++)
        {
            var nativePreset = GetNativePreset((uint)i);

            var isAlt = altContentId > 0 && nativePreset.ContentFinderConditionId == altContentId;
            if (isAlt)
                nativePreset.ContentFinderConditionId = (ushort)altContentId;

            if (territoryId == 0
                || contentId > 0 && nativePreset.ContentFinderConditionId == contentId
                || isAlt)
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

    public IEnumerable<(int, WaymarkPreset)> ListSavedPresets(ushort territoryId = 0)
    {
        uint altTerritoryId = 0;
        if (Plugin.Config.CombineEquivalentDutyPresets)
            equivalentTerritoryIds.TryGetValue(territoryId, out altTerritoryId);

        var presets = Plugin.Config.SavedPresets;
        for (int i = 0; i < presets.Count; i++)
        {
            var preset = presets[i];

            var isAlt = altTerritoryId > 0 && preset.TerritoryId == altTerritoryId;
            if (isAlt)
            {
                preset.TerritoryId = territoryId;
                preset.ContentFinderConditionId = TerritorySheet.GetContentId(territoryId);
            }

            if (territoryId == 0
                || preset.TerritoryId == territoryId
                || isAlt)
                yield return (i, preset);
        }
    }

    public void SavePreset(WaymarkPreset preset)
    {
        Plugin.Config.SavedPresets.Add(preset);
        Plugin.Config.Save();
    }

    public void DeleteSavedPreset(int index)
    {
        Plugin.Config.SavedPresets.RemoveAt(index);
        Plugin.Config.Save();
    }

    public IEnumerable<WaymarkPreset> ListCommunityPresets(ushort territoryId = 0)
    {
        var presets = Enumerable.Empty<WaymarkPreset>();
        if (CommunityPresets.TerritoryToPreset.TryGetValue(territoryId, out var communityPresets))
            presets = presets.Concat(communityPresets);

        if (Plugin.Config.CombineEquivalentDutyPresets && equivalentTerritoryIds.TryGetValue(territoryId, out var altTerritoryId))
            if (CommunityPresets.TerritoryToPreset.TryGetValue((ushort)altTerritoryId, out var altCommunityPresets))
            {
                altCommunityPresets.ForEach(preset =>
                {
                    preset.TerritoryId = territoryId;
                    preset.ContentFinderConditionId = TerritorySheet.GetContentId(territoryId);
                });
                presets = presets.Concat(altCommunityPresets);
            }

        return presets;
    }
}
