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
    private readonly Hook<AgentFieldMarker.Delegates.Show>? show;
    private readonly Hook<AgentFieldMarker.Delegates.Hide>? hide;

    int lastHover = -1;

    public FieldMarkerAddon()
    {
        //show ??= Plugin.Hooker.HookFromAddress<AgentFieldMarker.Delegates.Show>((nint)AgentFieldMarker.Instance()->VirtualTable->Show, OnShow);
        //hide ??= Plugin.Hooker.HookFromAddress<AgentFieldMarker.Delegates.Hide>((nint)AgentFieldMarker.Instance()->VirtualTable->Hide, OnHide);

        Plugin.AddonLifecycle.RegisterListener(AddonEvent.PostDraw, "FieldMarker", AddonPostDraw);
    }
    public void Dispose()
    {
        //show?.Dispose();
        //hide?.Dispose();
        Plugin.AddonLifecycle.UnregisterListener(AddonPostDraw);
    }
    public void OnShow(AgentFieldMarker* thisPtr)
    {
        Plugin.Chat.Print("show");
        if (thisPtr == null)
            return;
    }
    public void OnHide(AgentFieldMarker* thisPtr)
    {
        Plugin.Chat.Print("hide");
        if (thisPtr == null)
            return;
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
                var gamePreset = Plugin.Storage.GetNativePreset(thisPtr->SelectedPage, (uint)thisPtr->HoveredPresetIndex);
                Plugin.WaymarkManager.SetHoverPreview(gamePreset.ToPreset());
            }
            else
                Plugin.WaymarkManager.ClearHoverPreview();
            lastHover = thisPtr->HoveredPresetIndex;
        }
    }
}
