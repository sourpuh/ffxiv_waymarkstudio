using ImGuiNET;
using System;
using System.Numerics;

namespace WaymarkStudio.Windows;

internal class LibraryWindow : BaseWindow
{
    private readonly Vector2 headerSize = new(20);
    private readonly Vector2 filterIconButtonSize = new(24);
    private TerritoryFilter filter = new();

    internal LibraryWindow() : base("Waymark Studio Library")
    {
        Size = new(370, 500);
        SizeCondition = ImGuiCond.Once;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new(250, 330),
        };
    }

    public unsafe override void MyDraw()
    {
        DrawLibrary();
    }

    internal void DrawLibrary()
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

        var library = Plugin.Storage.GetPresetLibrary(filter);
        if (ImGui.BeginTable("saved_presets", 1, ImGuiTableFlags.BordersOuter))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            if (library.IsEmpty)
            {
                ImGui.Text("No Presets Found");
            }
            foreach ((var territoryId, var presetList) in library)
            {
                TerritoryHeader(territoryId);
                if (ImGui.BeginTable("" + territoryId, 2, ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.SizingFixedSame))
                {
                    ImGui.TableSetupColumn("NameButton", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Buttons");
                    ImGui.Indent();
                    foreach ((var i, var preset) in presetList)
                        DrawPresetRow(i, preset, isRenaming: renameIndex == i);
                    ImGui.Unindent();
                    ImGui.EndTable();
                }
            }
            ImGui.EndTable();
        }

        /*
        ImGui.SameLine();

        if (ImGui.BeginTable("native_presets", 2, ImGuiTableFlags.BordersOuter | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.SizingFixedSame | ImGuiTableFlags.RowBg))
        {
            var nativePresets = Plugin.Storage.ListNativePresets();
            if (nativePresets.Any())
            {
                foreach ((var j, var nativePreset) in nativePresets)
                {
                    var name = TerritorySheet.GetContentName(nativePreset.ContentFinderConditionId);
                    DrawPresetRow(j, nativePreset.ToPreset($"{j + 1}. {name}"), isReadOnly: true);
                }
            }
            ImGui.EndTable();
        }*/
    }

    private void TerritoryHeader(uint territoryId)
    {
        ImGui.Separator();
        MyGui.ExpansionIcon(territoryId, headerSize);
        ImGui.SameLine();
        MyGui.ContentTypeIcon(territoryId, headerSize);
        ImGui.SameLine();
        ImGui.SetWindowFontScale(1.1f);
        ImGui.Text(TerritorySheet.GetTerritoryName(territoryId));
        ImGui.SetWindowFontScale(1f);
    }
}
