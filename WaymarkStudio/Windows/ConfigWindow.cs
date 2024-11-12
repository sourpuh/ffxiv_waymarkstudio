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
        ImGui.Text("Did you think there would be something here?");
        /*
        if (ImGui.BeginTable("ConfigTable", 1, ImGuiTableFlags.BordersOuter | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.SizingFixedSame))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            if (ImGui.Checkbox("Share Criterion Normal and Savage Presets", ref Configuration.ShareAcrossDifficulties))
            {
            }
            ImGui.EndTable();
        }
        */
    }
}
