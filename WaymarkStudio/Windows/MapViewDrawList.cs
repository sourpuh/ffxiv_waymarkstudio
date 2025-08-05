using Pictomancy;
using System.Numerics;

namespace WaymarkStudio.Windows;
internal partial class MapView
{
    public void AddText(Vector3 position, uint color, string text, float scale)
    {
        ImGui.GetWindowDrawList().AddText(W2S(position), color, text);
    }

    public void AddDot(Vector3 position, float radiusPixels, uint color, uint numSegments = 0)
    {
        ImGui.GetWindowDrawList().AddCircleFilled(W2S(position), radiusPixels, color);
    }

    public void PathLineTo(Vector3 point)
    {
        ImGui.GetWindowDrawList().PathLineTo(W2S(point));
    }

    public void PathArcTo(Vector3 point, float radius, float startAngle, float stopAngle, uint numSegments = 0)
    {
        ImGui.GetWindowDrawList().PathArcTo(W2S(point), radius * map.Scale, startAngle, stopAngle, (int)numSegments);
    }

    public void PathStroke(uint color, PctStrokeFlags flags = PctStrokeFlags.None, float thickness = 2)
    {
        var imflags = ImDrawFlags.None;
        if (flags is PctStrokeFlags.Closed)
            imflags = ImDrawFlags.Closed;
        ImGui.GetWindowDrawList().PathStroke(color, imflags, thickness);
    }

    public void AddSquare(Vector3 center, float halfWidth, uint color, float thickness = 2)
    {
        AddQuad(
            center + new Vector3(-halfWidth, 0, -halfWidth),
            center + new Vector3(halfWidth, 0, -halfWidth),
            center + new Vector3(halfWidth, 0, halfWidth),
            center + new Vector3(-halfWidth, 0, halfWidth),
            color,
            thickness
        );
    }

    public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, uint color, float thickness = 2)
    {
        ImGui.GetWindowDrawList().AddQuad(W2S(a), W2S(b), W2S(c), W2S(d), color, thickness);
    }

    public void AddCircle(Vector3 origin, float radius, uint color, uint numSegments = 0, float thickness = 2)
    {
        ImGui.GetWindowDrawList().AddCircle(W2S(origin), radius * map.Scale, color, (int)numSegments, thickness);
    }
}
