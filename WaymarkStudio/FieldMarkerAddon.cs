using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System;

namespace WaymarkStudio;

/**
 * Extends the native Field Marker addon to display preview waymarks when mouse hovering over a preset.
 */
public unsafe class FieldMarkerAddon : IDisposable
{
    const uint Pages = 6;
    const uint EntriesPerPage = 5;
    private readonly Hook<AgentFieldMarker.Delegates.Show>? show;
    private int lastHover = -1;

    public FieldMarkerAddon()
    {
        show ??= Plugin.Hooker.HookFromAddress<AgentFieldMarker.Delegates.Show>((nint)AgentFieldMarker.Instance()->VirtualTable->Show, OnShow);
        show.Enable();

        Plugin.AddonLifecycle.RegisterListener(AddonEvent.PostDraw, "FieldMarker", AddonPostDraw);
    }
    public void Dispose()
    {
        show?.Disable();
        show?.Dispose();

        Plugin.AddonLifecycle.UnregisterListener(AddonPostDraw);
    }
    public void OnShow(AgentFieldMarker* thisPtr)
    {
        if (thisPtr == null)
            return;
        if (Plugin.Config.ReplaceNativeUi)
            Plugin.StudioWindow.Toggle();
        else
            show!.Original(thisPtr);
    }
    public void AddonPostDraw(AddonEvent type, AddonArgs args)
    {
        var thisPtr = (AddonFieldMarker*)args.Addon;
        if (thisPtr == null)
            return;

        if (lastHover != thisPtr->HoveredPresetIndex)
        {
            if (thisPtr->HoveredPresetIndex >= 0)
            {
                uint hoveredIndex = (uint)(thisPtr->SelectedPage * EntriesPerPage + thisPtr->HoveredPresetIndex);
                var gamePreset = Plugin.Storage.GetNativePreset(hoveredIndex);
                Plugin.WaymarkManager.SetHoverPreview(gamePreset.ToPreset());
            }
            else
                Plugin.WaymarkManager.ClearHoverPreview();
            lastHover = thisPtr->HoveredPresetIndex;
        }
    }
}
