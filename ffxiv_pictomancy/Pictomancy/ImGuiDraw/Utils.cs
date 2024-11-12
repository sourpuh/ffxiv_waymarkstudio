using Dalamud.Interface.Utility;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Pictomancy.ImGuiDraw;

internal static unsafe class Utils
{
    public enum LineClipStatus
    {
        NotVisible,
        NotClipped,
        A_Clipped,
        B_Clipped,
    }

    public static Vector3 XYZ(this Vector4 v) => new(v.X, v.Y, v.Z);
    public static LineClipStatus ClipLineToPlane(Vector4 plane, ref Vector3 a, ref Vector3 b, out float t)
    {
        t = 0f;
        var aDot = Vector4.Dot(new(a, 1), plane);
        var bDot = Vector4.Dot(new(b, 1), plane);
        bool aVis = aDot < 0;
        bool bVis = bDot < 0;
        if (aVis && bVis)
            return LineClipStatus.NotClipped;
        if (!aVis && !bVis)
            return LineClipStatus.NotVisible;

        Vector3 ab = b - a;
        t = -aDot / Vector3.Dot(ab, plane.XYZ());
        Vector3 abClipped = a + ab * t;
        if (aVis)
        {
            b = abClipped;
            return LineClipStatus.B_Clipped;
        }
        a = abClipped;
        return LineClipStatus.A_Clipped;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TransformCoordinate(in Vector3 coordinate, in Matrix4x4 transform, out Vector3 result)
    {
        result.X = (coordinate.X * transform.M11) + (coordinate.Y * transform.M21) + (coordinate.Z * transform.M31) + transform.M41;
        result.Y = (coordinate.X * transform.M12) + (coordinate.Y * transform.M22) + (coordinate.Z * transform.M32) + transform.M42;
        result.Z = (coordinate.X * transform.M13) + (coordinate.Y * transform.M23) + (coordinate.Z * transform.M33) + transform.M43;
        var w = 1f / ((coordinate.X * transform.M14) + (coordinate.Y * transform.M24) + (coordinate.Z * transform.M34) + transform.M44);
        result *= w;
    }

    public static Vector2 WorldToScreenOld(in Matrix4x4 viewProj, in Vector3 worldPos)
    {
        TransformCoordinate(worldPos, viewProj, out Vector3 viewPos);
        return new Vector2(
            0.5f * ImGuiHelpers.MainViewport.Size.X * (1 + viewPos.X),
            0.5f * ImGuiHelpers.MainViewport.Size.Y * (1 - viewPos.Y)) + ImGuiHelpers.MainViewport.Pos;
    }

    public static Vector2 WorldToScreen(in Matrix4x4 viewProj, in Vector3 worldPos)
    {
        var viewPos = Vector4.Transform(worldPos, viewProj);
        return new Vector2(
            0.5f * ImGuiHelpers.MainViewport.Size.X * (1 + viewPos.X),
            0.5f * ImGuiHelpers.MainViewport.Size.Y * (1 - viewPos.Y)) + ImGuiHelpers.MainViewport.Pos;
    }
}
