using Dalamud.Interface.Components;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Dalamud.Interface.Utility.Raii;

namespace WaymarkStudio.Windows;

public class ConfigWindow : Window
{
    private Configuration Configuration;

    public ConfigWindow() : base("Waymark Studio Config", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        Configuration = Plugin.Config;
    }

    public void Dispose() { }

    public override void Draw()
    {
        bool needSave = false;
        needSave |= ImGui.Checkbox("Replace Native Waymarks UI", ref Configuration.ReplaceNativeUi);
        needSave |= ImGui.Checkbox("Clear waymark when beginning placement", ref Configuration.ClearNativeWhenPlacing);
        MyGui.HoverTooltip("Clear already placed waymark when beginning the place the same waymark.\nFor example, when you click the 'A' button to start placing it, the 'A' waymark will be removed if it's already been placed.\nThe game's default UI clears waymarks, but '/waymark' does not.");
        needSave |= ImGui.Checkbox("Combine Equivalent Duty Presets (Criterion and Savage)", ref Configuration.CombineEquivalentDutyPresets);
        MyGui.HoverTooltip("Combine Criterion and Criterion Savage presets to use normal presets in savage and vice versa.");
        needSave |= ImGui.Checkbox("Disable World Preset Placement Safety Checks", ref Configuration.DisableWorldPresetSafetyChecks);
        MyGui.HoverTooltip("Disable distance, height, and frequency safety checks when placing presets in the open world / non-instance areas.\nWith this disabled, your preset placement will be obviously impossible; use at your own risk.");

        ImGui.Text("Libraries");
        using (ImRaii.PushIndent())
        {
            foreach (string x in new string[] { PresetStorage.WPP, PresetStorage.MM, PresetStorage.Native, PresetStorage.Community })
            {
                bool visible = Plugin.Config.IsLibraryVisible(x);
                if (VisibilityToggleButton(x, ref visible))
                {
                    Plugin.Config.SetLibraryVisibilty(x, visible);
                    needSave = true;
                }
            }
        }

        if (ImGui.Checkbox("[TESTING] Enable waymark VFX tracking", ref Configuration.EnableVfxTesting))
        {
            needSave = true;
            if (Configuration.EnableVfxTesting)
                Plugin.WaymarkVfx = new();
            else
                Plugin.WaymarkVfx = null;
        }
        MyGui.HoverTooltip("VFX tracking allows you to tune VFX visibility. There are no known issues with this feature, but it has caused the game to crash in the past. Use at your own risk; please report any issues.\nNote that waymarks placed before enabling this feature are not tracked and cannot be tuned.");

        if (needSave)
        {
            Configuration.Save();
        }
    }

    private bool VisibilityToggleButton(string name, ref bool visible)
    {
        bool wasVisible = visible;
        if (visible)
        {
            if (ImGuiComponents.IconButton(name, FontAwesomeIcon.EyeSlash))
                visible = false;
            MyGui.HoverTooltip($"Hide {name}");
        }
        else
        {
            if (ImGuiComponents.IconButton(name, FontAwesomeIcon.Eye))
                visible = true;
            MyGui.HoverTooltip($"Show {name}");
        }
        ImGui.SameLine();
        ImGui.Text(name);
        return visible != wasVisible;
    }
}
