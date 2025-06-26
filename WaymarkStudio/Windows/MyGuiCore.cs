using ImGuiNET;
using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace WaymarkStudio.Windows;
public partial class MyGui
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

    public static void TextActiveWaymarks(WaymarkPreset preset)
    {
        ImGui.SetWindowFontScale(1.2f);
        foreach (Waymark w in Enum.GetValues<Waymark>())
        {
            ImGui.PushStyleColor(ImGuiCol.Text, preset.MarkerPositions.ContainsKey(w) ? Waymarks.GetColor(w) : 0x70FFFFFF);
            ImGui.Text(Waymarks.GetName(w));
            if (w != Waymark.Four)
                ImGui.SameLine();
            ImGui.PopStyleColor();
        }
        ImGui.SetWindowFontScale(1f);
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

    public static void ExpansionIcon(ushort territoryId, Vector2 size)
    {
        var expansion = TerritorySheet.GetExpansion(territoryId);
        var expinfo = TerritorySheet.GetExpansionInfo(expansion);
        Icon(expinfo.icon, size, borderClip: 0.15f);
        HoverTooltip(expinfo.name);
    }

    public static void ContentTypeIcon(ushort territoryId, Vector2 size)
    {
        var ct = TerritorySheet.GetContentType(territoryId);
        var ctinfo = TerritorySheet.GetContentTypeInfo(ct);
        Icon(ctinfo.icon, size, borderClip: 0.1f);
        HoverTooltip(ctinfo.name);
    }

    internal static string GetCustomImagePath(string name)
    {
        return Path.Combine(Plugin.Interface.AssemblyLocation.Directory?.FullName!, "res", $"{name}.png");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static unsafe bool IsDropping(string name)
        => ImGui.AcceptDragDropPayload(name).NativePtr != null;
}
