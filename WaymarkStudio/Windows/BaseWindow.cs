using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.IO;
using System.Numerics;

namespace WaymarkStudio.Windows;
public abstract class BaseWindow : Window
{
    public BaseWindow(string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None) : base(name, flags) { }

    internal bool TextureIconButton(uint iconId, Vector2 size)
    {
        var wrap = Plugin.TextureProvider.GetFromGameIcon(iconId).GetWrapOrEmpty();
        if (wrap != null)
            return ImGui.ImageButton(wrap.ImGuiHandle, size, Vector2.Zero, Vector2.One, 1, Vector4.Zero);
        else
            return ImGui.Button("##" + iconId, size);
    }

    internal bool CustomTextureIconButton(string name, Vector2 size)
    {
        var wrap = Plugin.TextureProvider.GetFromFile(GetCustomImagePath(name)).GetWrapOrEmpty();
        if (wrap != null)
            return ImGui.ImageButton(wrap.ImGuiHandle, size, Vector2.Zero, Vector2.One, 1, Vector4.Zero);
        else
            return ImGui.Button("##" + name, size);
    }

    private string GetCustomImagePath(string name)
    {
        return Path.Combine(Plugin.Interface.AssemblyLocation.Directory?.FullName!, "res", $"{name}.png");
    }

    internal void HoverTooltip(string text)
    {
        HoverTooltip(() => ImGui.TextUnformatted(text));
    }

    internal void HoverTooltip(Action action)
    {
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            DisplayTooltip(action);
        }
    }

    private void DisplayTooltip(Action action)
    {
        ImGui.BeginTooltip();
        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
        action();
        ImGui.PopTextWrapPos();
        ImGui.EndTooltip();
    }
}
