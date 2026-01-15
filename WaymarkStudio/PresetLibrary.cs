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
    private bool sortByRecency;

    public PresetLibrary(Func<IEnumerable<WaymarkPreset>> getter, Func<bool> visibility, bool sortByRecency = false)
    {
        this.getter = getter;
        this.visibility = visibility;
        this.sortByRecency = sortByRecency;
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
        {
            cachedFilteredView = GetInternal(filter);
            lastFilter = filter;
        }
        return cachedFilteredView;
    }

    private LibraryView GetInternal(TerritoryFilter? filter = null)
    {
        var i = 0;
        var grouping = getter()
        .Select(preset => (index: i++, preset))
        .Where(preset => filter == null || !filter.Value.IsTerritoryFiltered(preset.Item2.TerritoryId))
        .GroupBy(preset => preset.Item2.TerritoryId, v => v);
        if (sortByRecency)
            return grouping.ToImmutableSortedDictionary(
                g => g.Key,
                g => g.OrderByDescending(e => e.preset.Time)
                      .AsEnumerable()
            );
        else
            return grouping.ToImmutableSortedDictionary(
                g => g.Key,
                g => g.AsEnumerable()
            );
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

    public bool ContainsEquivalentPreset(WaymarkPreset preset)
    {
        return ListPresets(preset.TerritoryId)
            .Any(p => p.Item2.IsEquivalent(preset));
    }
}
