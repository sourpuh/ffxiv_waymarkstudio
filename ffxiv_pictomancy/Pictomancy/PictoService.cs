﻿using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Pictomancy.DXDraw;
using Pictomancy.VfxDraw;

namespace Pictomancy;
#nullable disable

public class PictoService
{
    [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; }
    [PluginService] public static IGameGui GameGui { get; private set; }
    [PluginService] public static ICondition Condition { get; private set; }
    [PluginService] public static IPluginLog Log { get; private set; }
    [PluginService] public static ISigScanner SigScanner { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;

    private static DXRenderer _dxRenderer;
    public static VfxRenderer _vfxRenderer;
    private static AddonClipper _addonClipper;

    internal static PctDrawList DrawList;
    internal static PctDrawHints Hints;

    public static void Initialize(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<PictoService>();
        _vfxRenderer = new();
        _dxRenderer = new();
        _addonClipper = new();
        VfxFunctions.Initialize();
        Framework.Update += Update;
    }

    public static void Dispose()
    {
        _dxRenderer.Dispose();
        _vfxRenderer.Dispose();
        Framework.Update -= Update;
    }

    internal static void Update(IFramework framework)
    {
        _vfxRenderer.Update();
    }

    /// <summary>
    /// Helper for getting the current drawlist so it does not need to be passed around.
    /// Must be called within the scope of Draw().
    /// </summary>
    /// <returns>Current drawlist.</returns>
    /// <exception cref="InvalidOperationException">Throws when called before Draw()</exception>
    public static PctDrawList GetDrawList()
    {
        if (DrawList == null)
        {
            throw new InvalidOperationException("GetDrawList called without Draw");
        }
        return DrawList;
    }

    /// <summary>
    /// Begin drawing. Returns nullptr if drawing in unavailable.
    /// </summary>
    /// <param name="imguidrawlist">Optional ImGui drawlist. If unspecified, the background drawlist is used.</param>
    /// <param name="hints">Optional rendering hints. If unspecified, default hints are used.</param>
    /// <returns>Returns nullptr if drawing in unavailable.</returns>
    public static PctDrawList? Draw(ImDrawListPtr? imguidrawlist = null, PctDrawHints? hints = null)
    {
        Hints = hints ?? new();
        if (Hints.DrawInCutscene || IsInCutscene()) return null;
        if (Hints.DrawWhenFaded || IsFaded()) return null;

        return DrawList = new PctDrawList(
            imguidrawlist ?? ImGui.GetBackgroundDrawList(),
            _dxRenderer,
            _addonClipper
        );
    }

    private static bool IsInCutscene()
    {
        return Condition[ConditionFlag.OccupiedInCutSceneEvent] || Condition[ConditionFlag.WatchingCutscene78];
    }

    private unsafe static bool IsFaded()
    {
        var fadeMiddleWidget = (AtkUnitBase*)GameGui.GetAddonByName("FadeMiddle");
        var fadeBlackWidget = (AtkUnitBase*)GameGui.GetAddonByName("FadeBlack");
        return fadeMiddleWidget != null && fadeMiddleWidget->IsVisible ||
            fadeBlackWidget != null && fadeBlackWidget->IsVisible;
    }
}
