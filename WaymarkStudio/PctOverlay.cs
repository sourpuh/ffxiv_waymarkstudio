using FFXIVClientStructs.FFXIV.Client.System.Input;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using ImGuiNET;
using Pictomancy;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace WaymarkStudio;

/**
 * Pictomancy overlay to draw draft waymarks and guide and provide mouse world position selection.
 */
internal class PctOverlay
{
    object? currentMousePlacementThing;
    // TODO maybe Queue up everything to draw in this list if selection and placeholders should be separate
    List<Action<PctDrawList>> list = new();
    Vector3? lmb;
    Vector3? rmb;

    public PctOverlay()
    {
        PictoService.Initialize(Plugin.Interface);
        Plugin.Interface.UiBuilder.Draw += OnUpdate;
    }

    public void Dispose()
    {
        PictoService.Dispose();
        Plugin.Interface.UiBuilder.Draw -= OnUpdate;
    }

    private void DrawCircleMarker(PctDrawList drawList, Vector3 worldPos, uint color, uint glowColor)
    {
        drawList.AddCircle(worldPos, 1.25f, color);
        drawList.AddFanFilled(worldPos, 1.25f, 1.75f, 0, MathF.PI * 2, glowColor, glowColor.WithAlpha(0));
    }
    private void DrawSquareMarker(PctDrawList drawList, Vector3 worldPos, uint color, uint glowColor)
    {
        drawList.AddFan(worldPos, 0, 1.575f, MathF.PI / 4, MathF.PI * 2 + MathF.PI / 4, color, 4);
        drawList.AddFanFilled(worldPos, 1.575f, 2.1f, MathF.PI / 4, MathF.PI * 2 + MathF.PI / 4, glowColor, glowColor.WithAlpha(0), 4);
    }
    private void DrawMarkers(PctDrawList drawList, IReadOnlyDictionary<Waymark, Vector3> waymarkPositions)
    {
        foreach ((Waymark w, Vector3 p) in waymarkPositions)
        {
            if (w is Waymark.One or Waymark.Two or Waymark.Three or Waymark.Four)
                DrawSquareMarker(drawList, p, Waymarks.GetColor(w), Waymarks.GetGlowColor(w));
            if (w is Waymark.A or Waymark.B or Waymark.C or Waymark.D)
                DrawCircleMarker(drawList, p, Waymarks.GetColor(w), Waymarks.GetGlowColor(w));
        }
    }

    private void DrawCrosshair(PctDrawList drawList, Vector3 worldPos)
    {
        drawList.PathLineTo(worldPos - Vector3.UnitX);
        drawList.PathLineTo(worldPos + Vector3.UnitX);
        drawList.PathStroke(0xFFFFFFFF, new());
        drawList.PathLineTo(worldPos - Vector3.UnitZ);
        drawList.PathLineTo(worldPos + Vector3.UnitZ);
        drawList.PathStroke(0xFFFFFFFF, new());
    }

    private void DrawDebugMaterial(PctDrawList drawList, RaycastHit hit)
    {
        drawList.AddText(hit.Point, 0xFFFF00FF, hit.Material + "\n" + Convert.ToString((long)hit.Material, 2) + "\n" + hit.ComputeNormal(), 5);

        drawList.PathLineTo(hit.Point);
        drawList.PathLineTo(hit.Point + hit.ComputeNormal());
        drawList.PathStroke(0xFFFFFFFF, new());
    }

    public Vector3 SnapToGrid(Vector3 input)
    {
        input.X = MathF.Round(input.X);
        input.Z = MathF.Round(input.Z);
        return input;
    }

    internal void StartMouseWorldPosSelecting(object thing)
    {
        currentMousePlacementThing = thing;
    }

    internal enum SelectionResult
    {
        NotSelecting,
        SelectingValid,
        SelectingInvalid,
        Selected,
        Canceled,
    }

    internal unsafe SelectionResult MouseWorldPosSelection(object thing, ref Vector3 worldPos)
    {
        if (!thing.Equals(currentMousePlacementThing))
        {
            return SelectionResult.NotSelecting;
        }

        var mousePos = ImGui.GetIO().MousePos;
        if (Raycaster.ScreenToWorld(mousePos, out worldPos))
        {
            // TODO this should be in waymark manager so it can snap to guide
            if (Plugin.Config.SnapXZToGrid)
                worldPos = SnapToGrid(worldPos);
            worldPos = worldPos.Round();

            if (!Raycaster.CheckAndSnapY(ref worldPos))
                return SelectionResult.SelectingInvalid;

            // Use RMB to cancel selection if clicked with negligible drift.
            var rmbdown = UIInputData.Instance()->UIFilteredCursorInputs.MouseButtonHeldFlags.HasFlag(MouseButtonFlags.RBUTTON);
            if (rmbdown && rmb == null)
            {
                rmb = worldPos;
            }
            if (!rmbdown && rmb != null)
            {
                var drift = Vector3.DistanceSquared(worldPos, rmb.Value);
                rmb = null;
                if (drift < 1)
                {
                    currentMousePlacementThing = null;
                    return SelectionResult.Canceled;
                }
            }
            // Use LMB to confirm selection if clicked with negligible drift.
            var lmbdown = UIInputData.Instance()->UIFilteredCursorInputs.MouseButtonHeldFlags.HasFlag(MouseButtonFlags.LBUTTON);
            if (lmbdown && lmb == null)
            {
                lmb = worldPos;
            }
            if (!lmbdown && lmb != null)
            {
                var drift = Vector3.DistanceSquared(worldPos, lmb.Value);
                lmb = null;
                if (drift < 1)
                {
                    currentMousePlacementThing = null;
                    return SelectionResult.Selected;
                }
            }

            var worldPosTemp = worldPos;
            list.Add((PctDrawList drawList) => DrawCrosshair(drawList, worldPosTemp));
            return SelectionResult.SelectingValid;
        }

        return SelectionResult.SelectingInvalid;
    }

    bool once = true;

    private void OnUpdate()
    {
        if (once)
        {
            var shouldDraw =
                Plugin.StudioWindow.IsOpen
                && (Plugin.WaymarkManager.Placeholders.Count > 0
                || Plugin.WaymarkManager.HoverPreviews.Count > 0
                || Plugin.WaymarkManager.showGuide
                || list.Count > 0);
            if (!shouldDraw)
            {
                return;
            }
            try
            {
                using (var drawList = PictoService.Draw())
                {

                    if (drawList == null)
                        return;
                    foreach (var action in list)
                        action(drawList);
                    list.Clear();

                    DrawMarkers(drawList, Plugin.WaymarkManager.Placeholders);
                    DrawMarkers(drawList, Plugin.WaymarkManager.HoverPreviews);
                    if (Plugin.WaymarkManager.showGuide)
                        Plugin.WaymarkManager.guide.Draw(drawList);
                }
            }
            catch (Exception e)
            {
                Plugin.Chat.Print("caught " + e);
                once = false;
            }
        }
    }
}
