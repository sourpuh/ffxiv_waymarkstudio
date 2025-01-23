using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using WaymarkStudio.Guides;
using WaymarkStudio.Triggers;

namespace WaymarkStudio.Windows;

internal class StudioWindow : BaseWindow
{
    private readonly Vector2 waymarkIconButtonSize = new(30);
    private readonly Vector2 territoryInfoSize = new(20);
    private CircleTrigger? trigger;

    internal StudioWindow() : base("Waymark Studio")
    {
        Size = new(520, 480);
        SizeCondition = ImGuiCond.Once;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new(250, 330),
        };

        TitleBarButtons.Add(new() { ShowTooltip = () => ImGui.SetTooltip("Config"), Icon = FontAwesomeIcon.Cog, IconOffset = new(2, 1.5f), Click = _ => Plugin.ToggleConfigUI() });
        TitleBarButtons.Add(new() { ShowTooltip = () => ImGui.SetTooltip("Library"), Icon = FontAwesomeIcon.Atlas, IconOffset = new(2, 1.5f), Click = _ => Plugin.ToggleLibraryUI() });
        TitleBarButtons.Add(new() { ShowTooltip = () => ImGui.SetTooltip("Waymarks UI"), Icon = FontAwesomeIcon.ArrowUpRightFromSquare, IconOffset = new(2, 1.5f), Click = _ => Plugin.FieldMarkerAddon.Toggle() });
    }

    public override void Draw()
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
            /*
            if (ImGui.CollapsingHeader("Trigger", ImGuiTreeNodeFlags.DefaultOpen))
                using (ImRaii.Disabled(!Plugin.WaymarkManager.IsWaymarksEnabled()))
                    DrawTriggerSection();
            */

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
                if (MyGui.CustomTextureButton("circle_card", waymarkIconButtonSize))
                {
                    Plugin.WaymarkManager.PlaceWaymarkPlaceholder(Waymark.A, guide.North);
                    Plugin.WaymarkManager.PlaceWaymarkPlaceholder(Waymark.B, guide.East);
                    Plugin.WaymarkManager.PlaceWaymarkPlaceholder(Waymark.C, guide.South);
                    Plugin.WaymarkManager.PlaceWaymarkPlaceholder(Waymark.D, guide.West);
                }
                MyGui.HoverTooltip("Place Circles on guide cardinals");
            }

            WaymarkButton(Waymark.One); ImGui.SameLine();
            WaymarkButton(Waymark.Two); ImGui.SameLine();
            WaymarkButton(Waymark.Three); ImGui.SameLine();
            WaymarkButton(Waymark.Four); ImGui.SameLine();
            using (ImRaii.Disabled(!Plugin.WaymarkManager.showGuide))
            {
                var guide = Plugin.WaymarkManager.guide;
                if (MyGui.CustomTextureButton("square_intercard", waymarkIconButtonSize))
                {
                    Plugin.WaymarkManager.PlaceWaymarkPlaceholder(Waymark.One, guide.NorthWest);
                    Plugin.WaymarkManager.PlaceWaymarkPlaceholder(Waymark.Two, guide.NorthEast);
                    Plugin.WaymarkManager.PlaceWaymarkPlaceholder(Waymark.Three, guide.SouthEast);
                    Plugin.WaymarkManager.PlaceWaymarkPlaceholder(Waymark.Four, guide.SouthWest);
                }
                MyGui.HoverTooltip("Place Squares on guide intercardinals");
            }

            if (MyGui.IconButton(61502, waymarkIconButtonSize))
            {
                Plugin.WaymarkManager.ClearPlaceholders();
            }
            MyGui.HoverTooltip("Clear Draft");
            ImGui.SameLine();
            if (MyGui.IconButton(60026, waymarkIconButtonSize))
            {
                Plugin.WaymarkManager.ClearPlaceholders();
                Plugin.WaymarkManager.NativeClearWaymarks();
            }
            MyGui.HoverTooltip("Clear All");
        }
        using (ImRaii.Disabled(Plugin.WaymarkManager.placeholders.Count == 0))
        {
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Save, "Save Draft"))
            {
                var preset = Plugin.WaymarkManager.DraftPreset;
                preset.Name += $" {Plugin.Storage.CountPresetsForTerritoryId(Plugin.WaymarkManager.territoryId) + 1}";
                Plugin.Storage.SavePreset(preset);
            }
            MyGui.HoverTooltip("Save current draft to saved presets");
        }
        using (ImRaii.Disabled(Plugin.WaymarkManager.placeholders.Count == 0
            || !Plugin.WaymarkManager.IsSafeToPlaceWaymarks()))
        {
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.MapMarkedAlt, "Place Draft"))
            {
                Plugin.WaymarkManager.SafePlacePreset(Plugin.WaymarkManager.DraftPreset, mergeExisting: true);
            }
            MyGui.HoverTooltip("Replace draft markers with real markers");
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
            MyGui.HoverTooltip("Circle Guide");
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
            MyGui.HoverTooltip("Rectangle Guide");
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

    internal void DrawTriggerSection()
    {
        if (trigger == null && ImGuiComponents.IconButtonWithText(FontAwesomeIcon.LocationCrosshairs, "Add Trigger"))
        {
            trigger = new("New trigger", Plugin.WaymarkManager.territoryId);
            Plugin.Overlay.StartMouseWorldPosSelecting("trigger");
        }

        if (trigger == null)
            return;

        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.EyeSlash, "Remove Trigger"))
        {
            trigger = null;
            return;
        }

        ImGui.SameLine();
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Save, "Save"))
        {
            Plugin.Config.Triggers.Add((trigger, null));
            trigger = null;
            return;
        }

        ImGui.TextUnformatted("Position:");
        ImGui.SetNextItemWidth(125f);
        ImGui.SameLine();
        ImGui.InputFloat3("##position", ref trigger.Center, "%.1f");
        ImGui.SameLine();
        if (ImGuiComponents.IconButton("start_trigger_selection", FontAwesomeIcon.MousePointer))
        {
            Plugin.Overlay.StartMouseWorldPosSelecting("trigger");
        }
        switch (Plugin.Overlay.MouseWorldPosSelection("trigger", ref trigger.Center))
        {
            case PctOverlay.SelectionResult.Canceled:
                trigger = null;
                return;
        }

        ImGui.TextUnformatted("Radius:");
        ImGui.SetNextItemWidth(120f);
        ImGui.SameLine();
        ImGui.SliderFloat("##trigger_radius", ref trigger.Radius, 1, 20);

        trigger.Draw();
    }

    internal void DrawSavedPresets()
    {
        if (ImGui.BeginTable("PresetsTable", 1, ImGuiTableFlags.BordersOuter | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.SizingFixedSame | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY))
        {
            ImGui.TableSetupColumn("Core", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            MyGui.ExpansionIcon(Plugin.WaymarkManager.territoryId, territoryInfoSize);
            ImGui.SameLine();
            MyGui.ContentTypeIcon(Plugin.WaymarkManager.territoryId, territoryInfoSize);
            ImGui.SameLine();
            ImGui.Text($"{Plugin.WaymarkManager.mapName}");
            var currentMarkers = Plugin.WaymarkManager.WaymarkPreset;
            TextActiveWaymarks(currentMarkers);
            ImGui.SameLine();
            using (ImRaii.Disabled(currentMarkers.MarkerPositions.Count == 0))
            {
                if (ImGuiComponents.IconButton("save_markers", FontAwesomeIcon.Save))
                {
                    currentMarkers.Name += $" {Plugin.Storage.CountPresetsForTerritoryId(Plugin.WaymarkManager.territoryId) + 1}";
                    Plugin.Storage.SavePreset(currentMarkers);
                }
                MyGui.HoverTooltip("Save markers to presets");
                ImGui.SameLine();
                if (ImGuiComponents.IconButton("draftify_markers", FontAwesomeIcon.MapMarkerAlt))
                {
                    foreach ((Waymark w, Vector3 p) in Plugin.WaymarkManager.Waymarks)
                        Plugin.WaymarkManager.PlaceWaymarkPlaceholder(w, p);
                }
                MyGui.HoverTooltip("Import markers as draft");
                ImGui.SameLine();
                using (ImRaii.Disabled(!Plugin.WaymarkManager.IsSafeToPlaceWaymarks()))
                {
                    if (ImGuiComponents.IconButton("clear_markers", FontAwesomeIcon.Times))
                    {
                        Plugin.WaymarkManager.NativeClearWaymarks();
                    }
                    MyGui.HoverTooltip("Clear Waymarks");
                }
            }
            /*
            ImGui.Text("Triggers");
            foreach ((var trigger, var preset) in Plugin.Config.Triggers)
            {
                var name = trigger.Name;
                if (preset != null)
                    name += $" {preset.Name}";
                ImGui.Selectable(name);
            }
            */

            ImGui.Text("Saved Presets");
            ImGui.SameLine();
            ImguiFFLogsImportButton();
            ClipboardImportButton();

            DrawPresetList("mainListView", Plugin.Storage.ListSavedPresets(Plugin.WaymarkManager.territoryId));
            var nativePresets = Plugin.Storage.ListNativePresets(Plugin.WaymarkManager.territoryId);
            if (nativePresets.Any())
            {
                ImGui.Text($"Native Presets");
                DrawPresetList("nativeListView",
                    Plugin.Storage.ListNativePresets(Plugin.WaymarkManager.territoryId).Select(x => (x.Item1, x.Item2.ToPreset($"Slot {x.Item1 + 1}"))),
                    readOnly: true);
            }
            var communityPresets = Plugin.Storage.ListCommunityPresets(Plugin.WaymarkManager.territoryId);
            if (communityPresets.Any())
            {
                ImGui.Text($"Community Presets");
                DrawPresetList("communityListView",
                    Plugin.Storage.ListCommunityPresets(Plugin.WaymarkManager.territoryId),
                    readOnly: true);
            }

            ImGui.EndTable();
        }
    }

    internal void WaymarkButton(Waymark w)
    {
        if (MyGui.IconButton(Waymarks.GetIconId(w), waymarkIconButtonSize))
        {
            Plugin.Overlay.StartMouseWorldPosSelecting(w);
            if (Plugin.Config.ClearNativeWhenPlacing && Plugin.Config.PlaceRealIfPossible)
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
        MyGui.HoverTooltip("Rotate 90° Clockwise");
    }
}
