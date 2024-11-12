using FFXIVClientStructs.FFXIV.Client.Game.Control;
using ImGuiNET;
using System.Numerics;
using static Pictomancy.ImGuiDraw.Utils;

namespace Pictomancy.ImGuiDraw;
internal class ImGuiRenderer
{
    readonly ImDrawListPtr drawList;
    readonly Matrix4x4 viewProj;
    readonly Vector4 nearPlane;

    public unsafe ImGuiRenderer(ImDrawListPtr drawList)
    {
        this.drawList = drawList;
        viewProj = Control.Instance()->ViewProjectionMatrix;

        // The view matrix in CameraManager is 1 frame stale compared to the Control viewproj matrix.
        // Computing the near plane using the stale view matrix results in clipping errors that look really bad when moving the camera.
        // Instead, compute the view matrix using the accurate viewproj matrix multiplied by the stale inverse proj matrix (Which rarely changes)
        var controlCamera = Control.Instance()->CameraManager.GetActiveCamera();
        var renderCamera = controlCamera != null ? controlCamera->SceneCamera.RenderCamera : null;
        if (renderCamera == null)
            return;
        var Proj = renderCamera->ProjectionMatrix;
        if (!Matrix4x4.Invert(Proj, out var InvProj))
            return;
        var View = viewProj * InvProj;

        nearPlane = new(View.M13, View.M23, View.M33, View.M43 + renderCamera->NearPlane);
    }

    public unsafe void DrawStroke(IEnumerable<Vector3> worldCoords, float thickness, uint color, bool closed = false)
    {
        var enumerator = worldCoords.GetEnumerator();
        if (!enumerator.MoveNext()) return;
        Vector3 first = enumerator.Current;
        Vector3 start = first;

        while (enumerator.MoveNext())
        {
            Vector3 stop = enumerator.Current;
            DrawStroke(start, stop, thickness, color);
            start = stop;
        }
        if (closed)
        {
            DrawStroke(start, first, thickness, color);
        }
    }

    public unsafe void DrawStroke(Vector3 start, Vector3 stop, float thickness, uint color)
    {
        var status = ClipLineToPlane(nearPlane, ref start, ref stop, out float _);
        if (status == LineClipStatus.NotVisible)
            return;
        drawList.PathClear();
        drawList.PathLineTo(WorldToScreenOld(viewProj, start));
        drawList.PathLineTo(WorldToScreenOld(viewProj, stop));
        drawList.PathStroke(color, ImDrawFlags.None, thickness);
    }
}
