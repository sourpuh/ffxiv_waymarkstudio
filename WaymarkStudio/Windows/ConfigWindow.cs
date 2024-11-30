using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;

namespace WaymarkStudio.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration;

    public ConfigWindow() : base("Waymark Studio Config")
    {

        Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;

        Configuration = Plugin.Config;
    }

    public void Dispose() { }

    public override void Draw()
    {
        bool needSave = false;
        needSave |= ImGui.Checkbox("Replace Native Waymarks UI", ref Configuration.ReplaceNativeUi);
        // needSave |= ImGui.Checkbox("Share Criterion Normal and Savage Presets", ref Configuration.ShareAcrossDifficulties);
        if (needSave)
        {
            Configuration.Save();
        }
    }
}
