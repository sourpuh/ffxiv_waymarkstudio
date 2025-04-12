using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using WaymarkStudio.Compat.WaymarkPresetPlugin;
using WaymarkStudio.FFLogs;
namespace WaymarkStudio.Windows;
public abstract class BaseWindow : Window
{
    internal FFLogsImport? import;

    public BaseWindow(string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None) : base(name, flags) { }

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

    internal void PresetTooltip(WaymarkPreset preset, bool readOnly = false)
    {
        if (preset.PendingHeightAdjustment.IsAnySet())
        {
            if (preset.IsCompatibleTerritory(Plugin.WaymarkManager.territoryId))
                ImGui.Text("(Enter area to complete import)");
            else if (!Plugin.WaymarkManager.IsPlayerWithinTraceDistance(preset))
                ImGui.Text("(Get closer to complete import)");
            else
                ImGui.Text("(Preset height adjustment failed - please report this issue)");
        }
        TextActiveWaymarks(preset);
        if (preset.Time > DateTimeOffset.MinValue)
            ImGui.TextUnformatted($"{preset.Time.ToLocalTime()} ({(preset.Time - DateTimeOffset.Now).ToString("%d")}d)");
        if (!readOnly)
        {
            ImGui.SetWindowFontScale(0.9f);
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey);
            ImGui.Text("(Drag to reorder)");
            ImGui.PopStyleColor();
            ImGui.SetWindowFontScale(1f);
        }
    }

    internal void DrawPresetList(string id, IEnumerable<(int, WaymarkPreset)> presetList, bool readOnly = false)
    {
        if (MyGui.BeginList(id, dragdroppable: !readOnly))
        {
            foreach ((var index, var preset) in presetList)
            {
                bool confirmDelete = false;
                if (MyGui.NextRow(index, preset.Name))
                {
                    var isSameTerritory = preset.IsCompatibleTerritory(Plugin.WaymarkManager.territoryId);
                    var canPlaceWaymark = Plugin.WaymarkManager.IsSafeToPlaceWaymarks() && isSameTerritory;

                    Vector4 buttonColor = new(1, 1, 1, 0.1f);
                    Vector4? hoveredColor = null;
                    Vector4? activeColor = null;
                    if (!canPlaceWaymark)
                    {
                        hoveredColor = ImGuiColors.DalamudGrey with { W = 0.2f };
                        activeColor = new();
                    }

                    if (MyGui.IsDraggingItem())
                    {
                        buttonColor = ImGuiColors.DalamudGrey with { W = 0.5f };
                    }

                    if (ImGuiComponents.IconButtonWithText(preset.GetIcon(), $"{preset.Name}##{index}",
                        size: new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight()),
                        defaultColor: buttonColor,
                        hoveredColor: hoveredColor,
                        activeColor: activeColor)
                        && canPlaceWaymark)
                        Plugin.WaymarkManager.SafePlacePreset(preset);

                    if (ImGui.IsItemHovered())
                    {
                        MyGui.DisplayTooltip(() => PresetTooltip(preset, readOnly));
                    }
                    if (MyGui.OnStartHover())
                    {
                        // Lazy solution until I add some sort of distance based trigger system.
                        // Adjust height when user hovers over the preset. Save if trace succeeded.
                        if (preset.PendingHeightAdjustment.IsAnySet())
                        {
                            Plugin.WaymarkManager.AdjustPresetHeight(preset);
                            if (!preset.PendingHeightAdjustment.IsAnySet())
                                Plugin.Config.Save();
                        }
                        if (!preset.PendingHeightAdjustment.IsAnySet())
                            Plugin.WaymarkManager.SetHoverPreview(preset);
                    }
                    if (MyGui.OnStopHover())
                    {
                        Plugin.WaymarkManager.ClearHoverPreview();
                    }

                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    {
                        ImGui.OpenPopup($"rightclick_popup_{id}##{index}");
                    }
                    if (ImGui.BeginPopup($"rightclick_popup_{id}##{index}"))
                    {
                        Vector2 size = new(150, ImGui.GetFrameHeight());

                        var closePopup = false;
                        if (!readOnly && ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Edit, "Rename", size: size, defaultColor: new()))
                        {
                            MyGui.StartRowRename();
                            closePopup = true;
                        }
                        if (!readOnly && ImGuiComponents.IconButtonWithText(FontAwesomeIcon.TrashAlt, "Delete", size: size, defaultColor: new()))
                        {
                            if (Plugin.Triggers.HasReference(preset))
                                confirmDelete = true;
                            else
                                MyGui.DeleteRow();
                            closePopup = true;
                        }
                        if (isSameTerritory)
                        {
                            if (!readOnly &&
                                Plugin.WaymarkManager.WaymarkPreset.MarkerPositions.Count > 0 &&
                                ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Sync, "Overwrite", size: size, defaultColor: new()))
                            {
                                preset.MarkerPositions = Plugin.WaymarkManager.WaymarkPreset.MarkerPositions.Clone();
                                Plugin.Config.Save();
                                closePopup = true;
                            }
                            if (!readOnly &&
                                Plugin.WaymarkManager.DraftPreset.MarkerPositions.Count > 0 &&
                                ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Sync, "Overwrite (From Draft)", size: size, defaultColor: new()))
                            {
                                preset.MarkerPositions = Plugin.WaymarkManager.DraftPreset.MarkerPositions.Clone();
                                Plugin.Config.Save();
                                closePopup = true;
                            }
                        }
                        if (!Plugin.Storage.ContainsEquivalentPreset(preset)
                            && ImGuiComponents.IconButtonWithText(FontAwesomeIcon.CartArrowDown, "Clone to WMS library", size: size, defaultColor: new()))
                        {
                            Plugin.Storage.SavePreset(preset.Clone());
                            closePopup = true;
                        }
                        using (ImRaii.Disabled(preset.PendingHeightAdjustment.IsAnySet()))
                            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.FileExport, "Export to clipboard", size: size, defaultColor: new()))
                            {
                                ImGui.SetClipboardText(preset.Export());
                                closePopup = true;
                            }
                        if (isSameTerritory)
                        {
                            using (ImRaii.Disabled(!canPlaceWaymark))
                                if (ImGuiComponents.IconButtonWithText(preset.GetIcon(), "Place Preset", size: size, defaultColor: new()))
                                {
                                    Plugin.WaymarkManager.SafePlacePreset(preset);
                                    closePopup = true;
                                }
                            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.MapMarkerAlt, "Load as Draft", size: size, defaultColor: new()))
                            {
                                Plugin.WaymarkManager.SetPlaceholderPreset(preset);
                                closePopup = true;
                            }
                        }
                        if (closePopup)
                            ImGui.CloseCurrentPopup();

                        ImGui.EndPopup();
                    }
                    if (confirmDelete)
                        ImGui.OpenPopup($"deletereference_popup_{id}##{index}");
                    if (ImGui.BeginPopup($"deletereference_popup_{id}##{index}"))
                    {
                        ImGui.Text("This preset is linked to a Trigger. Delete anyway?");
                        if (ImGuiComponents.IconButton("accept_deletereference", FontAwesomeIcon.Check))
                        {
                            Plugin.Triggers.DeleteReferences(preset);
                            MyGui.DeleteRow();
                            ImGui.CloseCurrentPopup();
                        }
                        ImGui.SameLine();
                        if (ImGuiComponents.IconButton("cancel_deletereference", FontAwesomeIcon.Times))
                        {
                            ImGui.CloseCurrentPopup();
                        }
                        ImGui.EndPopup();
                    }
                    MyGui.EndRow();
                }

                if (MyGui.OnRename(out string newName))
                {
                    preset.Name = newName;
                    Plugin.Config.Save();
                }
            }
            if (MyGui.OnMove(out int source, out int target))
            {
                // Plugin.Chat.Print("Move s:" + source + " t:" + target);
                Plugin.Storage.MovePreset(source, target);
            }
            if (MyGui.OnDelete(out int deleteIndex))
            {
                // Plugin.Chat.Print("Delete " + deleteIndex);
                Plugin.Storage.DeleteSavedPreset(deleteIndex);
            }
            MyGui.EndList();
        }
    }

    internal void ClipboardImportButton()
    {
        string clipboard = ImGui.GetClipboardText();
        if (clipboard.StartsWith(WaymarkPreset.presetb64Prefix))
        {
            ImGui.SameLine();
            if (ImGuiComponents.IconButton("import_preset", FontAwesomeIcon.FileImport))
            {
                var preset = WaymarkPreset.Import(clipboard);
                Plugin.Storage.SavePreset(preset);
                ImGui.SetClipboardText("");
                Plugin.Chat.Print($"Successfully imported {preset.Name} for {TerritorySheet.GetTerritoryName(preset.TerritoryId)}.", Plugin.Tag, 45);
            }
            MyGui.HoverTooltip("Import From clipboard");
        }
        else if (clipboard.StartsWith("{"))
        {
            ImGui.SameLine();
            if (ImGuiComponents.IconButton("import_preset", FontAwesomeIcon.FileImport))
            {
                try
                {
                    var preset = WPPImporter.Import(clipboard);
                    Plugin.Storage.SavePreset(preset);
                    ImGui.SetClipboardText("");
                    Plugin.Chat.Print($"Successfully imported {preset.Name} for {TerritorySheet.GetTerritoryName(preset.TerritoryId)}.", Plugin.Tag, 45);
                }
                catch (Exception ex)
                {
                    Plugin.Chat.PrintError($"Waymark preset import failed. Check if your clipboard contains a valid JSON preset and try again. Message: \"{ex.Message}\"", Plugin.Tag);
                }
            }
            MyGui.HoverTooltip("Import From clipboard");
        }
    }

    internal void ImguiFFLogsImportButton()
    {
        bool focusTextInput = false;
        ImGui.PushStyleColor(ImGuiCol.Text, 0xFFE1C864);
        if (ImGuiComponents.IconButton("import_fflogs", FontAwesomeIcon.Gem))
        {
            ImGui.OpenPopup("fflogs_import_popup");
            focusTextInput = true;
        }
        ImGui.PopStyleColor();
        MyGui.HoverTooltip("Import From FFLogs");

        if (ImGui.BeginPopup("fflogs_import_popup"))
        {
            if (import == null)
                import = FFLogsImport.New("Paste FFLogs link here");

            // Url Input
            if (focusTextInput) ImGui.SetKeyboardFocusHere(0);
            ImGui.Text("FFLogs Report URL:");
            ImGui.SameLine();
            var runQueryReport = ImGui.InputText("##query_report", ref import.URL, 100, ImGuiInputTextFlags.EnterReturnsTrue);
            if (!import.IsStarted)
            {
                using (ImRaii.Disabled(!import.CanQuery))
                {
                    if (ImGuiComponents.IconButton("query_report", FontAwesomeIcon.Check) || runQueryReport)
                    {
                        import.Start();
                    }
                }
            }

            // Fight dropdown
            if (import.FightArray.Length > 0)
            {
                ImGui.Text("Fight:"); ImGui.SameLine();
                ImGui.Combo("##fight", ref import.UserSelectedFightIndex, import.FightArray, import.FightArray.Length);

                using (ImRaii.Disabled(!import.CanQuery))
                {
                    if (ImGuiComponents.IconButton("query_fight", FontAwesomeIcon.Check))
                    {
                        import.Continue();
                    }
                }
            }

            if (import.IsStarted)
            {
                if (import.Task.IsCompletedSuccessfully)
                {
                    var preset = import.Task.Result;
                    Plugin.Storage.SavePreset(preset);
                }
                else if (import.Task.IsFaulted)
                {
                    Plugin.Chat.PrintError(import.Task.Exception.Message, Plugin.Tag);
                    Plugin.Log.Error(import.Task.Exception.ToString());
                }
                else
                {
                    ImGui.Text("Import in progress. Please wait.");
                }
            }

            ImGui.SameLine();
            if (ImGuiComponents.IconButton("cancel_query", FontAwesomeIcon.Times) || import.IsCompleted)
            {
                import = null;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }
}
