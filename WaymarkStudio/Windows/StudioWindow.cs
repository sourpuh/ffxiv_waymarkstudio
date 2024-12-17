using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using WaymarkStudio.Guides;

namespace WaymarkStudio.Windows;

internal class StudioWindow : BaseWindow
{
    private readonly Vector2 iconButtonSize = new(30, 30);
    bool isHoverPreview = false;
    bool wasHoverPreview = false;
    string popupRename = "";
    int deleteIndex = -1;
    int renameIndex = -1;
    string renamingPresetName = "";
    bool renameFocus = false;

    internal StudioWindow()
        : base("Waymark Studio")
    {
        Size = new(555, 505);
        SizeCondition = ImGuiCond.Once;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new(250, 330),
        };

        TitleBarButtons.Add(new() { ShowTooltip = () => ImGui.SetTooltip("Config"), Icon = FontAwesomeIcon.Cog, IconOffset = new(2, 1.5f), Click = _ => Plugin.ToggleConfigUI() });
        TitleBarButtons.Add(new() { ShowTooltip = () => ImGui.SetTooltip("Waymarks UI"), Icon = FontAwesomeIcon.Atlas, IconOffset = new(2, 1.5f), Click = _ => Plugin.FieldMarkerAddon.Toggle() });
    }

    public unsafe override void Draw()
    {
        isHoverPreview = false;

        DrawStudio();
        /*
        ImGui.BeginTabBar("TabBar");
        if (ImGui.BeginTabItem("Studio"))
        {
            DrawStudio();
            ImGui.EndTabItem();
        }
        if (ImGui.BeginTabItem("Library"))
        {
            DrawLibrary();
            ImGui.EndTabItem();
        }
        ImGui.EndTabItem();
        ImGui.EndTabBar();
        */

        if (wasHoverPreview && !isHoverPreview)
            Plugin.WaymarkManager.ClearHoverPreview();
        wasHoverPreview = isHoverPreview;
    }

    internal void DrawStudio()
    {
        if (ImGui.BeginTable("StudioTable", 1, ImGuiTableFlags.BordersOuter | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.SizingFixedSame))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            if (ImGui.CollapsingHeader("Draft", ImGuiTreeNodeFlags.DefaultOpen))
                using (ImRaii.Disabled(!Plugin.WaymarkManager.IsWaymarksEnabled()))
                    DrawDraftSection();

            ImGui.Spacing();

            if (ImGui.CollapsingHeader("Guide", ImGuiTreeNodeFlags.DefaultOpen))
                using (ImRaii.Disabled(!Plugin.WaymarkManager.IsWaymarksEnabled()))
                    DrawGuideSection();

            ImGui.Spacing();
            ImGui.EndTable();
        }
        ImGui.SameLine();
        DrawSavedPresets();
    }

    internal void DrawDraftSection()
    {
        ImGui.Checkbox("Place real marker if possible", ref Plugin.Config.PlaceRealIfPossible);
        ImGui.Checkbox("Snap to grid", ref Plugin.Config.SnapXZToGrid);

        using (ImRaii.Disabled(!Plugin.WaymarkManager.IsWaymarksEnabled()))
        {
            WaymarkButton(Waymark.A); ImGui.SameLine();
            WaymarkButton(Waymark.B); ImGui.SameLine();
            WaymarkButton(Waymark.C); ImGui.SameLine();
            WaymarkButton(Waymark.D); ImGui.SameLine();
            using (ImRaii.Disabled(!Plugin.WaymarkManager.showGuide))
            {
                var guide = Plugin.WaymarkManager.guide;
                if (CustomTextureIconButton("circle_card", iconButtonSize))
                {
                    Plugin.WaymarkManager.PlaceWaymarkPlaceholder(Waymark.A, guide.North);
                    Plugin.WaymarkManager.PlaceWaymarkPlaceholder(Waymark.B, guide.East);
                    Plugin.WaymarkManager.PlaceWaymarkPlaceholder(Waymark.C, guide.South);
                    Plugin.WaymarkManager.PlaceWaymarkPlaceholder(Waymark.D, guide.West);
                }
                HoverTooltip("Place Circles on guide cardinals");
            }

            WaymarkButton(Waymark.One); ImGui.SameLine();
            WaymarkButton(Waymark.Two); ImGui.SameLine();
            WaymarkButton(Waymark.Three); ImGui.SameLine();
            WaymarkButton(Waymark.Four); ImGui.SameLine();
            using (ImRaii.Disabled(!Plugin.WaymarkManager.showGuide))
            {
                var guide = Plugin.WaymarkManager.guide;
                if (CustomTextureIconButton("square_intercard", iconButtonSize))
                {
                    Plugin.WaymarkManager.PlaceWaymarkPlaceholder(Waymark.One, guide.NorthWest);
                    Plugin.WaymarkManager.PlaceWaymarkPlaceholder(Waymark.Two, guide.NorthEast);
                    Plugin.WaymarkManager.PlaceWaymarkPlaceholder(Waymark.Three, guide.SouthEast);
                    Plugin.WaymarkManager.PlaceWaymarkPlaceholder(Waymark.Four, guide.SouthWest);
                }
                HoverTooltip("Place Squares on guide intercardinals");
            }

            if (TextureIconButton(61502, iconButtonSize))
            {
                Plugin.WaymarkManager.ClearPlaceholders();
            }
            HoverTooltip("Clear Draft");
            ImGui.SameLine();
            if (TextureIconButton(60026, iconButtonSize))
            {
                Plugin.WaymarkManager.ClearPlaceholders();
                Plugin.WaymarkManager.NativeClearWaymarks();
            }
            HoverTooltip("Clear All");
        }
        using (ImRaii.Disabled(Plugin.WaymarkManager.placeholders.Count == 0))
        {
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Save, "Save Draft"))
            {
                var preset = Plugin.WaymarkManager.DraftPreset;
                preset.Name += $" {Plugin.Storage.CountPresetsForTerritoryId(Plugin.WaymarkManager.territoryId) + 1}";
                Plugin.Config.SavedPresets.Add(preset);
                Plugin.Config.Save();
            }
            HoverTooltip("Save current draft to saved presets");
        }
        using (ImRaii.Disabled(Plugin.WaymarkManager.placeholders.Count == 0
            || !Plugin.WaymarkManager.IsSafeToPlaceWaymarks()))
        {
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.MapMarkedAlt, "Place Draft"))
            {
                Plugin.WaymarkManager.SafePlacePreset(Plugin.WaymarkManager.DraftPreset, mergeNative: true);
            }
            HoverTooltip("Replace draft markers with real markers");
        }
    }

    internal void DrawGuideSection()
    {
        if (Plugin.WaymarkManager.showGuide && ImGuiComponents.IconButtonWithText(FontAwesomeIcon.EyeSlash, "Hide Guide"))
        {
            Plugin.WaymarkManager.showGuide = false;
        }
        else if (!Plugin.WaymarkManager.showGuide)
        {
            if (Plugin.WaymarkManager.guide.center == Vector3.Zero)
            {
                if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.LocationCrosshairs, "Place Guide"))
                {
                    Plugin.WaymarkManager.showGuide = true;
                    Plugin.Overlay.StartMouseWorldPosSelecting("rectangleGuide");
                }
            }
            else if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Eye, "Show Guide"))
            {
                Plugin.WaymarkManager.showGuide = true;
            }
        }
        ImGui.SameLine();

        using (ImRaii.Disabled(Plugin.WaymarkManager.guide is CircleGuide))
        {
            if (ImGuiComponents.IconButton("circle_guide", FontAwesomeIcon.Bullseye))
            {
                var oldGuide = Plugin.WaymarkManager.guide;
                if (oldGuide is RectangleGuide oldRectangleGuide)
                {
                    var newGuide = new CircleGuide(Math.Max(oldRectangleGuide.HalfWidth, oldRectangleGuide.HalfDepth));
                    newGuide.center = oldRectangleGuide.center;
                    newGuide.RotationDegrees = oldRectangleGuide.RotationDegrees;
                    Plugin.WaymarkManager.guide = newGuide;
                }
            }
            HoverTooltip("Circle Guide");
        }
        ImGui.SameLine();
        using (ImRaii.Disabled(Plugin.WaymarkManager.guide is RectangleGuide))
        {
            if (ImGuiComponents.IconButton("rectangle_guide", FontAwesomeIcon.BorderAll))
            {
                var oldGuide = Plugin.WaymarkManager.guide;
                if (oldGuide is CircleGuide oldCircleGuide)
                {
                    var newGuide = new RectangleGuide(oldCircleGuide.Radius, oldCircleGuide.Radius);
                    newGuide.center = oldCircleGuide.center;
                    newGuide.RotationDegrees = oldCircleGuide.RotationDegrees;
                    Plugin.WaymarkManager.guide = newGuide;
                }
            }
            HoverTooltip("Rectangle Guide");
        }

        ImGui.TextUnformatted("Position:");
        ImGui.SetNextItemWidth(125f);
        ImGui.SameLine();
        ImGui.InputFloat3("##position", ref Plugin.WaymarkManager.guide.center, "%.1f");
        ImGui.SameLine();
        if (ImGuiComponents.IconButton("start_guide_selection", FontAwesomeIcon.MousePointer))
        {
            Plugin.WaymarkManager.showGuide = true;
            Plugin.Overlay.StartMouseWorldPosSelecting("rectangleGuide");
        }
        switch (Plugin.Overlay.MouseWorldPosSelection("rectangleGuide", ref Plugin.WaymarkManager.guide.center))
        {
            case PctOverlay.SelectionResult.Canceled:
                Plugin.WaymarkManager.showGuide = false;
                break;
        }

        if (Plugin.WaymarkManager.guide is CircleGuide circleGuide)
        {
            ImGui.TextUnformatted("Radius:");
            ImGui.SetNextItemWidth(120f);
            ImGui.SameLine();
            ImGui.SliderInt("##radius", ref circleGuide.Radius, 1, 20);

            ImGui.TextUnformatted("Spokes:");
            ImGui.SetNextItemWidth(120f);
            ImGui.SameLine();
            ImGui.SliderInt("##spokes", ref circleGuide.Spokes, 0, 16);

            ImGui.TextUnformatted("Rings:");
            ImGui.SetNextItemWidth(120f);
            ImGui.SameLine();
            ImGui.SliderInt("##rings", ref circleGuide.Rings, 1, 10);

            ImGui.TextUnformatted("Rotation:");
            ImGui.SetNextItemWidth(120f);
            ImGui.SameLine();
            ImguiRotationInput(ref circleGuide.RotationDegrees);
        }
        if (Plugin.WaymarkManager.guide is RectangleGuide rectangleGuide)
        {
            ImGui.TextUnformatted("Width:");
            ImGui.SetNextItemWidth(120f);
            ImGui.SameLine();
            ImGui.SliderInt("##width", ref rectangleGuide.HalfWidth, 1, 20);

            ImGui.TextUnformatted("Depth:");
            ImGui.SetNextItemWidth(120f);
            ImGui.SameLine();
            ImGui.SliderInt("##depth", ref rectangleGuide.HalfDepth, 1, 20);

            ImGui.TextUnformatted("Grid Size:");
            ImGui.SetNextItemWidth(120f);
            ImGui.SameLine();
            ImGui.SliderInt("##gridSize", ref rectangleGuide.GridSize, 1, 5);

            ImGui.TextUnformatted("Rotation:");
            ImGui.SetNextItemWidth(120f);
            ImGui.SameLine();
            ImguiRotationInput(ref rectangleGuide.RotationDegrees);
        }
    }

    internal void DrawSavedPresets()
    {
        if (ImGui.BeginTable("PresetsTable", 2, ImGuiTableFlags.BordersOuter | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.SizingFixedSame | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text($"{Plugin.WaymarkManager.mapName}");
            var currentMarkers = Plugin.WaymarkManager.WaymarkPreset;
            TextActiveWaymarks(currentMarkers);
            ImGui.SameLine();
            using (ImRaii.Disabled(currentMarkers.MarkerPositions.Count == 0))
            {
                if (ImGuiComponents.IconButton("save_markers", FontAwesomeIcon.Save))
                {
                    Plugin.Config.SavedPresets.Add(currentMarkers);
                    Plugin.Config.Save();
                }
                HoverTooltip("Save markers to presets");
                ImGui.SameLine();
                if (ImGuiComponents.IconButton("draftify_markers", FontAwesomeIcon.MapMarkerAlt))
                {
                    foreach ((Waymark w, Vector3 p) in Plugin.WaymarkManager.Waymarks)
                        Plugin.WaymarkManager.PlaceWaymarkPlaceholder(w, p);
                }
                HoverTooltip("Import markers as draft");
                ImGui.SameLine();
                using (ImRaii.Disabled(!Plugin.WaymarkManager.IsSafeToPlaceWaymarks()))
                {
                    if (ImGuiComponents.IconButton("clear_markers", FontAwesomeIcon.Times))
                    {
                        Plugin.WaymarkManager.NativeClearWaymarks();
                    }
                    HoverTooltip("Clear Waymarks");
                }
            }
            ImGui.Text("Saved Presets");
            ImGui.SameLine();
            string clipboard = ImGui.GetClipboardText();
            if (clipboard.StartsWith(WaymarkPreset.presetb64Prefix))
            {
                if (ImGuiComponents.IconButton("import_preset", FontAwesomeIcon.FileImport))
                {
                    var preset = WaymarkPreset.Import(clipboard);
                    Plugin.Config.SavedPresets.Add(preset);
                    Plugin.Config.Save();
                    ImGui.SetClipboardText("");
                }
                HoverTooltip("Import From clipboard");
            }

            deleteIndex = -1;

            foreach ((var i, var preset) in Plugin.Storage.ListSavedPresets(Plugin.WaymarkManager.territoryId))
                DrawPresetRow(i, preset);

            if (deleteIndex >= 0)
                Plugin.Storage.DeleteSavedPreset(deleteIndex);

            var nativePresets = Plugin.Storage.ListNativePresets(Plugin.WaymarkManager.territoryId);
            if (nativePresets.Any())
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text($"\nNative Presets");
                foreach ((var j, var nativePreset) in nativePresets)
                    DrawPresetRow(j, nativePreset.ToPreset($"{j + 1}. Game Preset"), isReadOnly: true);
            }

            var communityPresets = Plugin.Storage.ListCommunityPresets(Plugin.WaymarkManager.territoryId);
            if (communityPresets.Any())
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text($"\nCommunity Presets");
                int i = 0;
                foreach (var preset in communityPresets)
                    DrawPresetRow(i++, preset, isReadOnly: true);
            }
            ImGui.EndTable();
        }
    }

    internal void DrawLibrary()
    {
        deleteIndex = -1;

        var map = Plugin.Storage.ListSavedPresets().GroupBy(preset => preset.Item2.TerritoryId, v => v).ToDictionary(g => g.Key, g => g.ToList());

        if (ImGui.BeginTable("saved_presets", 1, ImGuiTableFlags.BordersOuter | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.SizingFixedSame))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            foreach ((var territoryId, var presetList) in map)
            {
                if (ImGui.CollapsingHeader(Plugin.Storage.GetTerritoryName(territoryId), ImGuiTreeNodeFlags.DefaultOpen))
                {
                    if (ImGui.BeginTable("" + territoryId, 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.RowBg))
                    {
                        foreach ((var i, var preset) in presetList)
                            DrawPresetRow(i, preset);
                        ImGui.EndTable();
                    }
                }
            }
            ImGui.EndTable();
        }

        if (deleteIndex >= 0)
            Plugin.Storage.DeleteSavedPreset(deleteIndex);


        ImGui.SameLine();

        if (ImGui.BeginTable("native_presets", 2, ImGuiTableFlags.BordersOuter | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.SizingFixedSame | ImGuiTableFlags.RowBg))
        {
            var nativePresets = Plugin.Storage.ListNativePresets();
            if (nativePresets.Any())
            {
                foreach ((var j, var nativePreset) in nativePresets)
                {
                    var name = Plugin.Storage.GetContentName(nativePreset.ContentFinderConditionId);
                    DrawPresetRow(j, nativePreset.ToPreset($"{j + 1}. {name}"), isReadOnly: true);
                }
            }
            ImGui.EndTable();
        }
    }

    internal void DrawPresetRow(int i, WaymarkPreset preset, bool isReadOnly = false)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();

        if (preset.TerritoryId == 0)
        {
            ImGui.Text(preset.Name);
            ImGui.TableNextColumn();
            return;
        }

        var isSameTerritory = preset.TerritoryId == Plugin.WaymarkManager.territoryId;
        var canPlaceWaymark = Plugin.WaymarkManager.IsSafeToPlaceWaymarks() && isSameTerritory;
        var isRenaming = renameIndex == i;

        if (isRenaming)
        {
            if (renameFocus)
            {
                ImGui.SetKeyboardFocusHere(0);
                renameFocus = false;
            }

            ImGui.SetNextItemWidth(220);
            var result = ImGui.InputText("##preset_rename", ref renamingPresetName, 50, ImGuiInputTextFlags.EnterReturnsTrue);
            bool isUnfocused = false; //ImGui.IsItemDeactivated();
            ImGui.TableNextColumn();
            if (ImGuiComponents.IconButton("accept_rename", FontAwesomeIcon.Check) || result)
            {
                if (renamingPresetName.Length > 0)
                {
                    preset.Name = renamingPresetName;
                    Plugin.Config.Save();
                }
                renameIndex = -1;
            }
            ImGui.SameLine();
            if (ImGuiComponents.IconButton("cancel_rename", FontAwesomeIcon.Times) || isUnfocused)
            {
                renameIndex = -1;
            }
        }
        else
        {
            Vector4? hoveredColor = null;
            Vector4? activeColor = null;
            if (!canPlaceWaymark)
            {
                hoveredColor = new();
                activeColor = new();
            }

            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.MapMarkedAlt, preset.Name + "##" + i,
                size: new(220, ImGui.GetFrameHeight()),
                defaultColor: new(),
                hoveredColor: hoveredColor,
                activeColor: activeColor)
                && canPlaceWaymark)
            {
                Plugin.WaymarkManager.SafePlacePreset(preset);
            }
            isHoverPreview |= HoverWaymarkPreview(preset);
            HoverTooltip(() =>
            {
                TextActiveWaymarks(preset);
                if (preset.Time > DateTimeOffset.MinValue)
                    ImGui.TextUnformatted($"{preset.Time.ToLocalTime()} ({(preset.Time - DateTimeOffset.Now).ToString("%d")}d)");
            });

            if (!isReadOnly)
            {
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    ImGui.OpenPopup($"rightclick_popup##{i}");
                }
                if (ImGui.BeginPopup($"rightclick_popup##{i}"))
                {
                    Vector2 size = new(150, ImGui.GetFrameHeight());
                    if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Edit, "Rename", size: size, defaultColor: new()))
                    {
                        ImGui.CloseCurrentPopup();
                        renameIndex = i;
                        renamingPresetName = preset.Name;
                        renameFocus = true;
                    }
                    if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.TrashAlt, "Delete", size: size, defaultColor: new()))
                    {
                        ImGui.CloseCurrentPopup();
                        deleteIndex = i;
                    }
                    if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.FileExport, "Export to clipboard", size: size, defaultColor: new()))
                    {
                        ImGui.CloseCurrentPopup();
                        ImGui.SetClipboardText(preset.Export());
                    }
                    ImGui.EndPopup();
                }
            }
            ImGui.TableNextColumn();
            if (isSameTerritory)
            {
                if (ImGuiComponents.IconButton($"draft_preset##{i}", FontAwesomeIcon.MapMarkerAlt))
                {
                    Plugin.WaymarkManager.SetPlaceholderPreset(preset);
                }
                isHoverPreview |= HoverWaymarkPreview(preset);
                HoverTooltip("Load as draft");
            }
        }
    }

    internal void WaymarkButton(Waymark w)
    {
        if (TextureIconButton(Waymarks.GetIconId(w), iconButtonSize))
        {
            Plugin.Overlay.StartMouseWorldPosSelecting(w);
            if (Plugin.Config.PlaceRealIfPossible)
                Plugin.WaymarkManager.NativeClearWaymark(w);
        }
        Vector3 pos = Plugin.WaymarkManager.placeholders.GetValueOrDefault(w);
        switch (Plugin.Overlay.MouseWorldPosSelection(w, ref pos))
        {
            case PctOverlay.SelectionResult.Canceled:
                Plugin.WaymarkManager.ClearWaymarkPlaceholder(w);
                break;
            case PctOverlay.SelectionResult.Selected:
                if (Plugin.Config.PlaceRealIfPossible
                    && Plugin.WaymarkManager.SafePlaceWaymark(w, pos))
                {
                    Plugin.WaymarkManager.ClearWaymarkPlaceholder(w);
                    break;
                }
                goto case PctOverlay.SelectionResult.SelectingValid;
            case PctOverlay.SelectionResult.SelectingValid:
                Plugin.WaymarkManager.PlaceWaymarkPlaceholder(w, pos);
                break;
            case PctOverlay.SelectionResult.SelectingInvalid:
                Plugin.WaymarkManager.ClearWaymarkPlaceholder(w);
                break;
        }
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            if (Plugin.Config.PlaceRealIfPossible)
                Plugin.WaymarkManager.NativeClearWaymark(w);
            else
                Plugin.WaymarkManager.ClearWaymarkPlaceholder(w);
        }
    }

    internal static bool HoverWaymarkPreview(WaymarkPreset preset)
    {
        if (ImGui.IsItemHovered())
        {
            Plugin.WaymarkManager.SetHoverPreview(preset);
            return true;
        }
        return false;
    }

    internal void TextActiveWaymarks(WaymarkPreset preset)
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

    internal void ImguiRotationInput(ref int rotationDegrees)
    {
        ImGui.DragInt("##rotation", ref rotationDegrees, 15, -180, 180);
        ImGui.SameLine();
        if (ImGuiComponents.IconButton("rotate_guide", FontAwesomeIcon.LevelDownAlt))
        {
            rotationDegrees += 90;
            if (rotationDegrees > 180)
                rotationDegrees -= 360;
        }
        HoverTooltip("Rotate 90° Clockwise");
    }
}
