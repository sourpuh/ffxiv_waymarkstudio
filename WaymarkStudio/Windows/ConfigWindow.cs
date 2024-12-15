using ImGuiNET;

namespace WaymarkStudio.Windows;

public class ConfigWindow : BaseWindow
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
        needSave |= ImGui.Checkbox("Combine Equivalent Duty Presets (Criterion and Savage)", ref Configuration.CombineEquivalentDutyPresets);
        HoverTooltip("Combine Criterion and Criterion Savage presets to use normal presets in savage and vice versa.\nUsing this setting will cause these presets to change which duty they belong to; this only matters if you disable the setting or share the preset to someone with it disabled.");
        if (needSave)
        {
            Configuration.Save();
        }
    }
}
