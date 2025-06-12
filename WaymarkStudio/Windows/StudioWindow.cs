using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using WaymarkStudio.Guides;

namespace WaymarkStudio.Windows;

internal class StudioWindow : BaseWindow
{
    private readonly Vector2 waymarkIconButtonSize = new(30);
    private readonly Vector2 territoryInfoSize = new(20);
    private Vector2 windowPosition;
    private Vector2 windowSize;
    internal StudioWindow() : base("Waymark Studio")
    {
        Size = new(520, 480);
        SizeCondition = ImGuiCond.Once;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new(250, 330),
        };

        TitleBarButtons.Add(new() { ShowTooltip = () => ImGui.SetTooltip("Config"), Icon = FontAwesomeIcon.Cog, IconOffset = new(2, 1.5f), Click = _ => Plugin.ToggleConfigUI() });
        // TitleBarButtons.Add(new() { ShowTooltip = () => ImGui.SetTooltip("Library"), Icon = FontAwesomeIcon.Atlas, IconOffset = new(2, 1.5f), Click = _ => Plugin.ToggleLibraryUI() });
        TitleBarButtons.Add(new() { ShowTooltip = () => ImGui.SetTooltip("Waymarks UI"), Icon = FontAwesomeIcon.ArrowUpRightFromSquare, IconOffset = new(2, 1.5f), Click = _ => Plugin.FieldMarkerAddon.Toggle() });
    }

    public override void Draw()
    {
        windowPosition = ImGui.GetWindowPos();
        windowSize = ImGui.GetWindowSize();

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
        if (ImGui.BeginTable("PresetsTable", 1, ImGuiTableFlags.BordersOuter | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.SizingFixedSame | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY))
        {
            ImGui.TableSetupColumn("Core", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            DrawMarkerInfo();
            ImGui.Separator();
            DrawSavedPresets();
            ImGui.EndTable();
        }
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
            IntySliderFloat("##radius", ref circleGuide.Radius, 1, 20);

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
            IntySliderFloat("##width", ref rectangleGuide.HalfWidth, 1, 20);

            ImGui.TextUnformatted("Depth:");
            ImGui.SetNextItemWidth(120f);
            ImGui.SameLine();
            IntySliderFloat("##depth", ref rectangleGuide.HalfDepth, 1, 20);

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

    internal void IntySliderFloat(string id, ref float val, int min, int max)
    {
        string fmt = (val - (int)val) > 0.05f ? "%.1f" : "%.0f";
        ImGui.SliderFloat(id, ref val, min, max, fmt);
    }

    internal void DrawMarkerInfo()
    {
        MyGui.ExpansionIcon(Plugin.WaymarkManager.territoryId, territoryInfoSize);
        ImGui.SameLine();
        MyGui.ContentTypeIcon(Plugin.WaymarkManager.territoryId, territoryInfoSize);
        ImGui.SameLine();
        ImGui.Text($"{Plugin.WaymarkManager.mapName}");
        var currentMarkers = Plugin.WaymarkManager.WaymarkPreset;
        TextActiveWaymarks(currentMarkers);
        //if (Plugin.WaymarkVfx.WaymarkAlpha <= 0)
        //{
        //    ImGui.SameLine();
        //    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 6f * ImGuiHelpers.GlobalScale);
        //    ImGui.SetWindowFontScale(0.7f);
        //    ImGui.PushFont(UiBuilder.IconFont);
        //    ImGui.PushStyleColor(ImGuiCol.Text, 0xFF2020FF);
        //    ImGui.Text(FontAwesomeIcon.EyeSlash.ToIconString());
        //    ImGui.PopStyleColor();
        //    ImGui.PopFont();
        //    ImGui.SetWindowFontScale(1f);
        //    MyGui.HoverTooltip("Waymarks are currently hidden");
        //}
        using (ImRaii.Disabled(currentMarkers.MarkerPositions.Count == 0))
        {
            if (ImGuiComponents.IconButton("save_markers", FontAwesomeIcon.Save))
            {
                currentMarkers.Name += $" {Plugin.Storage.CountPresetsForTerritoryId(Plugin.WaymarkManager.territoryId) + 1}";
                Plugin.Storage.SavePreset(currentMarkers);
            }
            MyGui.HoverTooltip("Save Waymarks to presets");
            ImGui.SameLine();
            if (ImGuiComponents.IconButton("draftify_markers", FontAwesomeIcon.MapMarkerAlt))
            {
                foreach ((Waymark w, Vector3 p) in Plugin.WaymarkManager.Waymarks)
                    Plugin.WaymarkManager.PlaceWaymarkPlaceholder(w, p);
            }
            MyGui.HoverTooltip("Import Waymarks as draft");
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
        //ImGui.SameLine();
        //{
        //    if (Plugin.WaymarkVfx.WaymarkAlpha > 0)
        //    {
        //        if (ImGuiComponents.IconButton("hide_markers", FontAwesomeIcon.EyeSlash))
        //        {
        //            Plugin.WaymarkVfx.WaymarkAlpha = 0;
        //        }
        //        MyGui.HoverTooltip("Hide Waymarks Locally\nRight click to adjust transparency");
        //    }
        //    else
        //    {
        //        if (ImGuiComponents.IconButton("show_markers", FontAwesomeIcon.Eye))
        //        {
        //            Plugin.WaymarkVfx.WaymarkAlpha = 1;
        //        }

        //        MyGui.HoverTooltip("Show Waymarks Locally\nRight click to adjust transparency");
        //    }
        //    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        //    {
        //        ImGui.OpenPopup("waymark_transparency_popup");
        //    }
        //    if (ImGui.BeginPopup("waymark_transparency_popup"))
        //    {
        //        var alpha = Plugin.WaymarkVfx.WaymarkAlpha;
        //        if (ImGui.SliderFloat("##alpha", ref alpha, 0, 1))
        //        {
        //            Plugin.WaymarkVfx.WaymarkAlpha = alpha;
        //        }
        //        ImGui.EndPopup();
        //    }
        //}
        ImGui.SameLine();
        ImGui.Spacing();
        ImGui.SameLine();
        using (ImRaii.Disabled(!Plugin.WaymarkManager.IsWaymarksEnabled()))
        {
            if (ImGuiComponents.IconButton("edit_triggers", FontAwesomeIcon.FlagCheckered))
            {
                Plugin.TriggerEditorWindow.Open(windowPosition, windowSize);
            }
            MyGui.HoverTooltip("Trigger Editor");
        }
        ImGui.SameLine();
        if (ImGuiComponents.IconButton("library", FontAwesomeIcon.Atlas))
        {
            Plugin.ToggleLibraryUI();
        }
        MyGui.HoverTooltip("Preset Library");
        ImGui.SameLine();
        ImguiFFLogsImportButton();
        ClipboardImportButton();
    }

    internal void DrawSavedPresets()
    {
        DrawTriggerList();

        ImGui.Text("Saved Presets");
        DrawPresetList("mainListView", Plugin.Storage.Library.ListPresets(Plugin.WaymarkManager.territoryId));
        if (Plugin.IsMMInstalled())
        {
            var mmPresets = Plugin.Storage.MMLibrary.ListPresets(Plugin.WaymarkManager.territoryId);
            if (mmPresets.Any())
            {
                ImGui.Text($"MemoryMarker Presets");
                DrawPresetList("nativeListView",
                    mmPresets,
                    readOnly: true);
            }
        }
        else
        {
            var nativePresets = Plugin.Storage.NativeLibrary.ListPresets(Plugin.WaymarkManager.territoryId);
            if (nativePresets.Any())
            {
                ImGui.Text($"Native Presets");
                DrawPresetList("nativeListView",
                    nativePresets,
                    readOnly: true);
            }
        }
        var communityPresets = Plugin.Storage.CommunityLibrary.ListPresets(Plugin.WaymarkManager.territoryId);
        if (communityPresets.Any())
        {
            ImGui.Text($"Community Presets");
            DrawPresetList("communityListView",
                communityPresets,
                readOnly: true);
        }
        if (Plugin.IsWPPInstalled())
        {
            var waymarkPresetPluginPresets = Plugin.Storage.WPPLibrary.ListPresets(Plugin.WaymarkManager.territoryId);
            if (waymarkPresetPluginPresets.Any())
            {
                ImGui.Text($"Waymark Preset Plugin Presets");
                DrawPresetList("wppListView",
                    waymarkPresetPluginPresets,
                    readOnly: true);
            }
        }
    }

    internal void DrawTriggerList()
    {
        var triggers = Plugin.Triggers.ListSavedTriggers(Plugin.WaymarkManager.territoryId);
        if (triggers.Any())
        {
            ImGui.Text("Triggers");
            if (MyGui.BeginList("triggers"))
            {
                foreach ((var index, var trigger, var preset) in triggers)
                {
                    trigger.Draw();

                    var name = trigger.Name;
                    if (MyGui.NextRow(index, name))
                    {
                        Vector4 buttonColor = new(1, 1, 1, 0.1f);
                        if (MyGui.IsDraggingItem())
                        {
                            buttonColor = ImGuiColors.DalamudGrey with { W = 0.5f };
                        }

                        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.FlagCheckered, $"{name}##{index}",
                            size: new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight()),
                            defaultColor: buttonColor))
                            Plugin.TriggerEditorWindow.Open(windowPosition, windowSize, trigger);

                        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                        {
                            ImGui.OpenPopup($"rightclick_trigger_popup##{index}");
                        }
                        if (ImGui.BeginPopup($"rightclick_trigger_popup##{index}"))
                        {
                            Vector2 size = new(150, ImGui.GetFrameHeight());

                            var closePopup = false;
                            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Edit, "Rename", size: size, defaultColor: new()))
                            {
                                MyGui.StartRowRename();
                                closePopup = true;
                            }
                            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.TrashAlt, "Delete", size: size, defaultColor: new()))
                            {
                                MyGui.DeleteRow();
                                closePopup = true;
                            }
                            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.FilePen, "Edit", size: size, defaultColor: new()))
                            {
                                Plugin.TriggerEditorWindow.Open(windowPosition, windowSize, trigger);
                                closePopup = true;
                            }
                            using (ImRaii.Disabled(preset == null))
                                if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Unlink, "Detach", size: size, defaultColor: new()))
                                {
                                    Plugin.Triggers.SaveTrigger(trigger, null);
                                    closePopup = true;
                                }
                            if (closePopup)
                                ImGui.CloseCurrentPopup();
                            ImGui.EndPopup();
                        }

                        if (ImGui.IsItemHovered())
                            MyGui.DisplayTooltip(() =>
                            {
                                if (preset != null)
                                    ImGui.Text($"Attached {preset.Name}");
                                else
                                    ImGui.Text("No attached preset");
                                ImGui.SetWindowFontScale(0.9f);
                                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey);
                                ImGui.Text("(Drag to reorder)");
                                ImGui.PopStyleColor();
                                ImGui.SetWindowFontScale(1f);
                            });

                        MyGui.EndRow();
                    }
                    if (MyGui.OnRename(out string newName))
                    {
                        trigger.Name = newName;
                        Plugin.Config.Save();
                    }
                }
                if (MyGui.OnMove(out int source, out int target))
                {
                    // Plugin.Chat.Print("Move s:" + source + " t:" + target);
                    Plugin.Triggers.MoveTrigger(source, target);
                }
                if (MyGui.OnDelete(out int deleteIndex))
                {
                    // Plugin.Chat.Print("Delete " + deleteIndex);
                    Plugin.Triggers.DeleteSavedTrigger(deleteIndex);
                }
                MyGui.EndList();
            }
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
