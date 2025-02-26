using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
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
    public object? currentMousePlacementThing;
    // TODO maybe Queue up everything to draw in this list if selection and placeholders should be separate
    internal List<Action<PctDrawList>> list = new();

    Quaternion? rmbStart;
    Quaternion? lmbStart;

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
    private void DrawMarkers(PctDrawList drawList, IReadOnlyDictionary<Waymark, Vector3> waymarkPositions, bool debugHeight = false)
    {
        foreach ((Waymark w, Vector3 p) in waymarkPositions)
        {
            var pa = p;

            if (debugHeight)
            {
                var castHeight = 100000f;
                if (Raycaster.CheckAndSnapY(ref pa, castHeight: castHeight))
                {
                    drawList.PathLineTo(p + new Vector3(0, castHeight / 2, 0));
                    drawList.PathLineTo(pa);
                    drawList.PathStroke(0xFFFFFFFF, new());
                }
            }

            if (w is Waymark.One or Waymark.Two or Waymark.Three or Waymark.Four)
                DrawSquareMarker(drawList, pa, Waymarks.GetColor(w), Waymarks.GetGlowColor(w));
            if (w is Waymark.A or Waymark.B or Waymark.C or Waymark.D)
                DrawCircleMarker(drawList, pa, Waymarks.GetColor(w), Waymarks.GetGlowColor(w));
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

    internal void DeferDrawDebugRay(RaycastHit hit1, RaycastHit hit2)
    {
        list.Add((PctDrawList drawList) => DrawDebugRay(drawList, hit1, hit2));
    }

    private void DrawDebugRay(PctDrawList drawList, RaycastHit hit1, RaycastHit hit2)
    {
        drawList.PathLineTo(hit1.Point);
        drawList.PathLineTo(hit2.Point);
        drawList.PathStroke(0xFF0000FF, new());
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

    private unsafe Quaternion CameraRotation => FFXIVClientStructs.FFXIV.Common.Math.Quaternion.CreateFromRotationMatrix(CameraManager.Instance()->CurrentCamera->ViewMatrix);

    private unsafe bool IsClicked(MouseButtonFlags button, ref Quaternion? startingCameraRotation)
    {
        // Use RMB to cancel selection if clicked with negligible drift.
        var isDown = UIInputData.Instance()->UIFilteredCursorInputs.MouseButtonHeldFlags.HasFlag(button);
        if (isDown && startingCameraRotation == null)
        {
            startingCameraRotation = CameraRotation;
        }
        if (!isDown && startingCameraRotation.HasValue)
        {
            var drift = 1 - Quaternion.Dot(CameraRotation, startingCameraRotation.Value);
            startingCameraRotation = null;
            if (drift < 0.001)
            {
                currentMousePlacementThing = null;
                return true;
            }
        }
        return false;
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

            if (IsClicked(MouseButtonFlags.RBUTTON, ref rmbStart))
            {
                return SelectionResult.Canceled;
            }

            if (IsClicked(MouseButtonFlags.LBUTTON, ref lmbStart))
            {
                return SelectionResult.Selected;
            }

            var worldPosTemp = worldPos;
            list.Add((PctDrawList drawList) => DrawCrosshair(drawList, worldPosTemp));
            return SelectionResult.SelectingValid;
        }

        return SelectionResult.SelectingInvalid;
    }

    public bool CanDraw => Plugin.StudioWindow.IsOpen;

    bool once = true;

    private void OnUpdate()
    {
        if (once)
        {
            var shouldDraw =
                CanDraw
                && (Plugin.WaymarkManager.Placeholders.Count > 0
                || Plugin.WaymarkManager.HoverPreviews.Count > 0
                || Plugin.WaymarkManager.showGuide
                || list.Count > 0);
            if (!shouldDraw)
            {
                list.Clear();
                return;
            }
            try
            {
                using (var drawList = PictoService.Draw())
                {

                    if (drawList == null)
                    {
                        list.Clear();
                        return;
                    }
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
                Plugin.Chat.PrintError($"Drawing Failed Please Report! Restart plugin to re-enable drawing. Caught {e}");
                once = false;
            }
        }
    }
}
