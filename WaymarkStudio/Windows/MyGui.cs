using ImGuiNET;
using System;
using System.IO;
using System.Numerics;

namespace WaymarkStudio.Windows;
public class MyGui
{
    public static void HoverTooltip(string text)
    {
        HoverTooltip(() => ImGui.TextUnformatted(text));
    }

    public static void HoverTooltip(Action action)
    {
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            DisplayTooltip(action);
    }

    public static void DisplayTooltip(Action action)
    {
        ImGui.BeginTooltip();
        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
        action();
        ImGui.PopTextWrapPos();
        ImGui.EndTooltip();
    }

    public static bool IconButton(uint iconId, Vector2 size, float borderClip = 0, bool state = true)
    {
        Vector4 tintColor = state ? new(1f, 1f, 1f, 1f) : new(0.5f, 0.5f, 0.5f, 0.85f);
        var wrap = Plugin.TextureProvider.GetFromGameIcon(iconId).GetWrapOrEmpty();
        if (wrap != null)
            return ImGui.ImageButton(wrap.ImGuiHandle, size, new Vector2(borderClip), Vector2.One - new Vector2(borderClip), 1, Vector4.Zero, tintColor);
        else
            return ImGui.Button("##" + iconId, size);
    }

    public static bool CustomTextureButton(string name, Vector2 size)
    {
        var wrap = Plugin.TextureProvider.GetFromFile(GetCustomImagePath(name)).GetWrapOrEmpty();
        if (wrap != null)
            return ImGui.ImageButton(wrap.ImGuiHandle, size, Vector2.Zero, Vector2.One, 1, Vector4.Zero);
        else
            return ImGui.Button("##" + name, size);
    }

    public static void Icon(uint iconId, Vector2 size, float borderClip = 0)
    {
        var wrap = Plugin.TextureProvider.GetFromGameIcon(iconId).GetWrapOrEmpty();
        if (wrap != null)
            ImGui.Image(wrap.ImGuiHandle, size, new Vector2(borderClip), Vector2.One - new Vector2(borderClip));
    }

    public static void ExpansionIcon(uint territoryId, Vector2 size)
    {
        var expansion = TerritorySheet.GetExpansion(territoryId);
        var expinfo = TerritorySheet.GetExpansionInfo(expansion);
        Icon(expinfo.icon, size, borderClip: 0.15f);
        HoverTooltip(expinfo.name);
    }

    public static void ContentTypeIcon(uint territoryId, Vector2 size)
    {
        var ct = TerritorySheet.GetContentType(territoryId);
        var ctinfo = TerritorySheet.GetContentTypeInfo(ct);
        Icon(ctinfo.icon, size, borderClip: 0.1f);
        HoverTooltip(ctinfo.name);
    }

    private static string GetCustomImagePath(string name)
    {
        return Path.Combine(Plugin.Interface.AssemblyLocation.Directory?.FullName!, "res", $"{name}.png");
    }
}
