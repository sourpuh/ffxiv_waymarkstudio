using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace WaymarkStudio;

internal class PresetLibrary
{
    private Func<IEnumerable<WaymarkPreset>> getter;
    private Func<bool> visibility;
    private TerritoryFilter lastFilter;
    private LibraryView? cachedFullView;
    private LibraryView? cachedFilteredView;

    public PresetLibrary(Func<IEnumerable<WaymarkPreset>> getter, Func<bool> visibility)
    {
        this.getter = getter;
        this.visibility = visibility;
    }

    public LibraryView Get(TerritoryFilter filter = default)
    {
        if (!visibility()) return LibraryView.Empty;

        if (filter == default)
        {
            if (cachedFullView == null)
                cachedFullView = GetInternal(filter);
            return cachedFullView;
        }

        if (cachedFilteredView == null || filter != lastFilter)
            cachedFilteredView = GetInternal(filter);
        return cachedFilteredView;
    }

    private LibraryView GetInternal(TerritoryFilter? filter = null)
    {
        var i = 0;
        return getter()
        .Select(x => (i++, x))
        .Where(preset => filter == null || !filter.Value.IsTerritoryFiltered(preset.Item2.TerritoryId))
        .GroupBy(preset => preset.Item2.TerritoryId, v => v)
        .ToImmutableSortedDictionary(g => g.Key, g => g.AsEnumerable());
    }

    public PresetList ListPresets(ushort territoryId)
    {
        var library = Get();
        var presets = Enumerable.Empty<(int, WaymarkPreset)>();
        if (library.TryGetValue(territoryId, out var presetList))
            presets = presets.Concat(presetList);

        var altTerritoryId = TerritorySheet.GetAlternativeId(territoryId);
        if (altTerritoryId != null)
            if (library.TryGetValue(altTerritoryId.Value, out var altPresetList))
                presets = presets.Concat(altPresetList);

        return presets;
    }

    public void InvalidateCache()
    {
        cachedFullView = null;
        cachedFilteredView = null;
    }
}
