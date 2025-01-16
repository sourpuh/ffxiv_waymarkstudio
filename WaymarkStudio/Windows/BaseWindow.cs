using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Linq;
using System.Numerics;
using WaymarkStudio.FFLogs;

namespace WaymarkStudio.Windows;
public abstract class BaseWindow : Window
{
    private bool isHoverPreview = false;
    private bool wasHoverPreview = false;
    private string popupRename = "";
    private int deleteIndex = -1;
    internal int renameIndex = -1;
    private string renamingPresetName = "";
    private bool renameFocus = false;
    internal FFLogsImport? import;

    public BaseWindow(string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None) : base(name, flags) { }

    public sealed override void Draw()
    {
        isHoverPreview = false;

        deleteIndex = -1;

        MyDraw();

        if (deleteIndex >= 0)
            Plugin.Storage.DeleteSavedPreset(deleteIndex);

        if (wasHoverPreview && !isHoverPreview)
            Plugin.WaymarkManager.ClearHoverPreview();
        wasHoverPreview = isHoverPreview;
    }

    public abstract void MyDraw();

    internal static bool HoverWaymarkPreview(WaymarkPreset preset)
    {
        if (ImGui.IsItemHovered())
        {
            // Lazy solution until I add some sort of distance based trigger system.
            // Adjust height when user hovers over the preset. Save if trace succeeded.
            if (preset.PendingHeightAdjustment.IsAnySet())
            {
                Plugin.WaymarkManager.AdjustPresetHeight(preset);
                if (!preset.PendingHeightAdjustment.IsAnySet())
                {
                    Plugin.Config.Save();
                }
                else return false;
            }
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

    internal void PresetTooltip(WaymarkPreset preset)
    {
        if (preset.PendingHeightAdjustment.IsAnySet())
        {
            if (preset.TerritoryId != Plugin.WaymarkManager.territoryId)
                ImGui.Text("(Enter area to complete import)");
            else if (!Plugin.WaymarkManager.IsPlayerWithinTraceDistance(preset))
                ImGui.Text("(Get closer to complete import)");
            else
                ImGui.Text("(Preset height adjustment failed - please report this issue)");
        }
        TextActiveWaymarks(preset);
        if (preset.Time > DateTimeOffset.MinValue)
            ImGui.TextUnformatted($"{preset.Time.ToLocalTime()} ({(preset.Time - DateTimeOffset.Now).ToString("%d")}d)");
    }

    internal void DrawPresetRow(int i, WaymarkPreset preset, bool isReadOnly = false, bool isRenaming = false)
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

        if (isRenaming)
        {
            if (renameFocus)
            {
                ImGui.SetKeyboardFocusHere(0);
                renameFocus = false;
            }

            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
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

            if (ImGuiComponents.IconButtonWithText(preset.GetIcon(), preset.Name + "##" + i,
                size: new(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight()),
                defaultColor: new(),
                hoveredColor: hoveredColor,
                activeColor: activeColor)
                && canPlaceWaymark)
                Plugin.WaymarkManager.SafePlacePreset(preset);

            isHoverPreview |= HoverWaymarkPreview(preset);
            MyGui.HoverTooltip(() => PresetTooltip(preset));

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
                    if (isSameTerritory)
                    {
                        if (Plugin.WaymarkManager.WaymarkPreset.MarkerPositions.Count > 0 &&
                            ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Sync, "Overwrite", size: size, defaultColor: new()))
                        {
                            preset.MarkerPositions = Plugin.WaymarkManager.WaymarkPreset.MarkerPositions
                                .ToDictionary(entry => entry.Key,
                                              entry => entry.Value);
                            Plugin.Config.Save();
                            ImGui.CloseCurrentPopup();
                        }
                        if (Plugin.WaymarkManager.DraftPreset.MarkerPositions.Count > 0 &&
                            ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Sync, "Overwrite (Draft)", size: size, defaultColor: new()))
                        {
                            preset.MarkerPositions = Plugin.WaymarkManager.DraftPreset.MarkerPositions
                                .ToDictionary(entry => entry.Key,
                                              entry => entry.Value);
                            Plugin.Config.Save();
                            ImGui.CloseCurrentPopup();
                        }
                    }
                    using (ImRaii.Disabled(preset.PendingHeightAdjustment.IsAnySet()))
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
                MyGui.HoverTooltip("Load as draft");
            }
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
                if (import.Task.IsFaulted)
                {
                    Plugin.Chat.PrintError(import.Task.Exception.Message, Plugin.Tag);
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
