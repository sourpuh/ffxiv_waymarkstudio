using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System.Linq;
using System.Numerics;
using WaymarkStudio.Triggers;

namespace WaymarkStudio.Windows;
internal class TriggerEditorWindow : Window
{
    private CircleTrigger? originalTrigger;
    private CircleTrigger? trigger;
    private int selectedPresetIndex = -1;
    private Vector3? startingPosition;

    internal TriggerEditorWindow() : base("Waymark Studio Trigger Editor", ImGuiWindowFlags.NoResize)
    {
        PositionCondition = ImGuiCond.Appearing;
        Size = new();
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new(100, 0),
        };
        AllowPinning = false;
    }

    public void Open(Vector2 parentPosition, Vector2 parentSize, CircleTrigger? trigger = null)
    {
        IsOpen = true;
        Position = parentPosition + parentSize with { Y = 0 };
        originalTrigger = trigger;
        this.trigger = null;
        if (trigger != null)
        {
            this.trigger = new(trigger);
            this.trigger.Editing = true;
        }
        selectedPresetIndex = -1;
        startingPosition = null;
    }

    public override void Draw()
    {
        if (trigger == null && ImGuiComponents.IconButtonWithText(FontAwesomeIcon.LocationCrosshairs, "Place Trigger"))
        {
            trigger = new("New trigger", Plugin.WaymarkManager.territoryId);
            trigger.Editing = true;
            Plugin.Overlay.StartMouseWorldPosSelecting("trigger");
        }

        if (trigger == null)
            return;

        ImGui.TextUnformatted("Name:");
        ImGui.SameLine();
        ImGui.InputText("##trigger_name", ref trigger.Name, 100);
        ImGui.TextUnformatted("Position:");
        ImGui.SetNextItemWidth(125f);
        ImGui.SameLine();
        ImGui.InputFloat3("##position", ref trigger.Center, "%.1f");
        ImGui.SameLine();
        if (ImGuiComponents.IconButton("start_trigger_selection", FontAwesomeIcon.MousePointer))
        {
            startingPosition = trigger.Center;
            Plugin.Overlay.StartMouseWorldPosSelecting("trigger");
        }
        switch (Plugin.Overlay.MouseWorldPosSelection("trigger", ref trigger.Center))
        {
            case PctOverlay.SelectionResult.Canceled:
                if (startingPosition.HasValue)
                    trigger.Center = startingPosition.Value;
                else
                    trigger = null;
                break;
        }

        ImGui.TextUnformatted("Radius:");
        ImGui.SetNextItemWidth(120f);
        ImGui.SameLine();
        ImGui.SliderFloat("##trigger_radius", ref trigger.Radius, 1, 20);

        var presets = Plugin.Storage.Library.ListPresets(Plugin.WaymarkManager.territoryId).Select(x => x.Item2).ToList();
        if (selectedPresetIndex == -1)
        {
            var attachedPreset = Plugin.Triggers.ActiveTriggers.Where(x => x.Item1 == originalTrigger).FirstOrDefault().Item2;
            selectedPresetIndex = presets.IndexOf(attachedPreset);
        }
        if (presets.Any())
        {
            ImGui.Text("Preset:"); ImGui.SameLine();
            var presetNames = presets.Select(x => x.Name).ToArray();
            ImGui.Combo("##preset", ref selectedPresetIndex, presetNames, presetNames.Length);
        }

        using (ImRaii.Disabled("trigger".Equals(Plugin.Overlay.currentMousePlacementThing)))
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Save, "Save and Close"))
            {
                if (originalTrigger != null)
                {
                    originalTrigger.CopyFrom(trigger);
                    trigger = originalTrigger;
                }

                var preset = selectedPresetIndex >= 0 ? presets[selectedPresetIndex] : null;
                Plugin.Triggers.SaveTrigger(trigger, preset);
                trigger.Editing = false;
                trigger = null;
                IsOpen = false;
                return;
            }
        ImGui.SameLine();
        if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Times, "Cancel"))
        {
            trigger.Editing = false;
            trigger = null;
            IsOpen = false;
            return;
        }

        trigger.Draw();
    }
}
