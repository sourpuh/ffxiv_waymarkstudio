using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.Interop;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using WaymarkStudio.Compat.WaymarkPresetPlugin;

namespace WaymarkStudio;

using PresetLibrary = ImmutableSortedDictionary<ushort, List<(int, WaymarkPreset)>>;

/**
 * This should handle storage management for plugin and native presets.
 */
internal class PresetStorage
{
    const uint MaxEntries = 30;
    private TerritoryFilter lastFilter;
    private PresetLibrary? cachedLibrary;
    private PresetLibrary? cachedWPPLibrary;
    private WPPConfiguration wppConfig;

    internal PresetStorage()
    {
        //wppConfig = WPPConfiguration.Load(Plugin.Interface);
    }

    public PresetLibrary GetPresetLibrary(TerritoryFilter filter)
    {
        if (cachedLibrary == null || filter != lastFilter)
            cachedLibrary = ListSavedPresets()
            .Where(preset => !filter.IsTerritoryFiltered(preset.Item2.TerritoryId))
            .GroupBy(preset => preset.Item2.TerritoryId, v => v)
            .ToImmutableSortedDictionary(g => g.Key, g => g.ToList());
        lastFilter = filter;
        return cachedLibrary;
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
        var contentId = TerritorySheet.GetContentId(territoryId);
        var altTerritoryId = TerritorySheet.GetAlternativeId(territoryId) ?? 0;
        var altContentId = TerritorySheet.GetContentId(altTerritoryId);

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
        var altTerritoryId = TerritorySheet.GetAlternativeId(territoryId);

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
        cachedLibrary = null;
        Plugin.Config.SavedPresets.Add(preset);
        SaveConfig();
    }

    public void DeleteSavedPreset(int index)
    {
        cachedLibrary = null;
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
        cachedLibrary = null;
        Plugin.Config.Save();
    }

    public IEnumerable<(int, WaymarkPreset)> ListCommunityPresets(ushort territoryId = 0)
    {
        var presets = Enumerable.Empty<(int, WaymarkPreset)>();
        if (CommunityPresets.TerritoryToPreset.TryGetValue(territoryId, out var communityPresets))
            presets = presets.Concat(communityPresets);

        var altTerritoryId = TerritorySheet.GetAlternativeId(territoryId);
        if (altTerritoryId != null)
            if (CommunityPresets.TerritoryToPreset.TryGetValue((ushort)altTerritoryId, out var altCommunityPresets))
            {
                altCommunityPresets.ForEach(preset =>
                {
                    preset.Item2.TerritoryId = territoryId;
                    preset.Item2.ContentFinderConditionId = TerritorySheet.GetContentId(territoryId);
                });
                presets = presets.Concat(altCommunityPresets);
            }
        return presets;
    }
    public IEnumerable<(int, WaymarkPreset)> ListWPPPresets(ushort territoryId = 0)
    {
        if (wppConfig != null)
        {
            int i = 0;
            foreach (var wppPreset in wppConfig.PresetLibrary.Presets)
            {
                var preset = wppPreset.ToPreset();
                if (preset.TerritoryId == territoryId)
                    yield return (i++, preset);
            }
        }
    }
}
