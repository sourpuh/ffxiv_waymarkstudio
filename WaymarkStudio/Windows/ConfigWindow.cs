using Dalamud.Interface.Windowing;
using ImGuiNET;

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
        MyGui.HoverTooltip("Combine Criterion and Criterion Savage presets to use normal presets in savage and vice versa.\nUsing this setting will cause these presets to change which duty they belong to; this only matters if you disable the setting or share the preset to someone with it disabled.");
        needSave |= ImGui.Checkbox("Disable World Preset Placement Safety Checks", ref Configuration.DisableWorldPresetSafetyChecks);
        MyGui.HoverTooltip("Disable distance, height, and frequency safety checks when placing presets in the open world / non-instance areas.\nWith this disabled, your preset placement will be obviously impossible; use at your own risk.");
        if (needSave)
        {
            Configuration.Save();
        }
    }
}
