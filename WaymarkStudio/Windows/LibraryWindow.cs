using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System;
using System.Collections.Immutable;
using System.Numerics;

namespace WaymarkStudio.Windows;

using LibraryView = ImmutableSortedDictionary<ushort, ImmutableList<(int, WaymarkPreset)>>;

internal class LibraryWindow : BaseWindow
{
    private readonly Vector2 headerSize = new(20);
    private readonly Vector2 filterIconButtonSize = new(24);
    private TerritoryFilter filter = new();

    internal LibraryWindow() : base("Waymark Studio Library", ImGuiWindowFlags.NoScrollbar)
    {
        Size = new(370, 500);
        SizeCondition = ImGuiCond.Once;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new(250, 330),
        };
    }

    public override void Draw()
    {
        foreach (var expansion in Enum.GetValues<Expansion>())
        {
            var expinfo = TerritorySheet.GetExpansionInfo(expansion);
            if (MyGui.IconButton(expinfo.icon, filterIconButtonSize, 0.1f, filter.SelectedExpansion == null || filter.SelectedExpansion == expansion))
                filter.Toggle(expansion);
            MyGui.HoverTooltip(expinfo.name);
            ImGui.SameLine();
        }
        ImGui.NewLine();
        foreach (var contenttype in Enum.GetValues<ContentType>())
        {
            var ctinfo = TerritorySheet.GetContentTypeInfo(contenttype);
            if (MyGui.IconButton(ctinfo.icon, filterIconButtonSize, 0.1f, filter.SelectedContentType == null || filter.SelectedContentType == contenttype))
                filter.Toggle(contenttype);
            MyGui.HoverTooltip(ctinfo.name);
            ImGui.SameLine();
        }
        ImGui.NewLine();
        using (var bar = ImRaii.TabBar("PresetBar"))
        {
            if (bar)
            {
                using (var tab = ImRaii.TabItem("WMS"))
                {
                    if (tab)
                        DrawLibrary(Plugin.Storage.Library.Get(filter));
                }
                if (Plugin.IsWPPInstalled())
                    using (var tab = ImRaii.TabItem("WPP"))
                    {
                        if (tab)
                            DrawLibrary(Plugin.Storage.WPPLibrary.Get(filter), readOnly: true);
                    }
                if (Plugin.IsMMInstalled())
                    using (var tab = ImRaii.TabItem("MemoryMarker"))
                    {
                        if (tab)
                            DrawLibrary(Plugin.Storage.MMLibrary.Get(filter), readOnly: true);
                    }
                else
                    using (var tab = ImRaii.TabItem("Native"))
                    {
                        if (tab)
                            DrawLibrary(Plugin.Storage.NativeLibrary.Get(filter), readOnly: true);
                    }
                using (var tab = ImRaii.TabItem("Community"))
                {
                    if (tab)
                        DrawLibrary(Plugin.Storage.CommunityLibrary.Get(filter), readOnly: true);
                }
            }
        }
    }

    private void DrawLibrary(LibraryView library, bool readOnly = false)
    {
        if (ImGui.BeginTable("saved_presets", 1, ImGuiTableFlags.BordersOuter | ImGuiTableFlags.ScrollY))
        {
            if (library.IsEmpty)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("No Presets Found");
            }
            foreach ((var territoryId, var presetList) in library)
            {
                ImGui.Separator();
                TerritoryHeader(territoryId);

                ImGui.Indent();
                DrawPresetList("" + territoryId, presetList, readOnly: readOnly);
                ImGui.Unindent();
            }
            ImGui.EndTable();
        }
    }

    private void TerritoryHeader(ushort territoryId)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        MyGui.ExpansionIcon(territoryId, headerSize);
        ImGui.SameLine();
        MyGui.ContentTypeIcon(territoryId, headerSize);
        ImGui.SameLine();
        ImGui.SetWindowFontScale(1.1f);
        ImGui.Text(TerritorySheet.GetTerritoryName(territoryId));
        ImGui.SetWindowFontScale(1f);
    }
}
