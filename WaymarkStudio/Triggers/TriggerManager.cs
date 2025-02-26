using System.Collections.Generic;
using System.Linq;

namespace WaymarkStudio.Triggers;
internal class TriggerManager
{
    private IList<(CircleTrigger, WaymarkPreset)>? cachedActiveTriggers;
    public IList<(CircleTrigger, WaymarkPreset)> ActiveTriggers
    {
        get
        {
            return cachedActiveTriggers ??= ListSavedTriggers(Plugin.ClientState.TerritoryType)
                .Where(x => x.Item3 != null)
                .Select(x => (x.Item2, x.Item3!))
                .ToList();
        }
    }

    internal void OnTerritoryChange()
    {
        cachedActiveTriggers = null;
    }

    public bool HasReference(WaymarkPreset preset)
    {
        foreach (var trigger in Plugin.Config.Triggers)
        {
            if (trigger.Item2 == preset) return true;
        }
        return false;
    }

    public void DeleteReferences(WaymarkPreset preset)
    {
        var triggers = Plugin.Config.Triggers;
        for (int i = 0; i < triggers.Count; i++)
        {
            var trigger = triggers[i];
            if (trigger.Item2 == preset)
            {
                trigger.Item2 = null;
                triggers[i] = trigger;
            }
        }
        cachedActiveTriggers = null;
        // Intentionally skip config save because this is only performed before preset deletion which also saves config.
    }

    public IEnumerable<(int, CircleTrigger, WaymarkPreset?)> ListSavedTriggers(ushort territoryId = 0)
    {
        var altTerritoryId = TerritorySheet.GetAlternativeId(territoryId) ?? 0;

        var triggers = Plugin.Config.Triggers;
        for (int i = 0; i < triggers.Count; i++)
        {
            var trigger = triggers[i];
            var isAlt = altTerritoryId > 0 && trigger.Item1.TerritoryId == altTerritoryId;

            if (territoryId == 0
                || trigger.Item1.TerritoryId == territoryId
                || isAlt)
                yield return (i, trigger.Item1, trigger.Item2);
        }
    }

    public void DeleteSavedTrigger(int index)
    {
        Plugin.Config.Triggers.RemoveAt(index);
        SaveConfig();
    }

    public void MoveTrigger(int sourceIndex, int targetIndex)
    {
        Plugin.Config.Triggers.Move(sourceIndex, targetIndex);
        SaveConfig();
    }

    public void SaveTrigger(CircleTrigger trigger, WaymarkPreset? preset = null)
    {
        int existingIndex = Plugin.Config.Triggers.FindIndex(x => x.Item1 == trigger);
        if (existingIndex == -1)
            Plugin.Config.Triggers.Add((trigger, preset));
        else
            Plugin.Config.Triggers[existingIndex] = (trigger, preset);
        SaveConfig();
    }

    private void SaveConfig()
    {
        cachedActiveTriggers = null;
        Plugin.Config.Save();
    }

    CircleTrigger? lastTrigger = null;
    public void Update()
    {
        foreach ((var trigger, var preset) in ActiveTriggers)
        {
            if (trigger.Contains(Plugin.ClientState.LocalPlayer))
            {
                if (lastTrigger != trigger)
                {
                    // Plugin.Chat.Print($"Place trigger {trigger.Name}");
                    Plugin.WaymarkManager.SafePlacePreset(preset);
                }
                lastTrigger = trigger;
                return;
            }
        }
        lastTrigger = null;
    }
}
