using Pictomancy;
using System;
using System.Numerics;

namespace WaymarkStudio.Guides;
public class RectangleGuide(int halfWidth = 1, int halfDepth = 1, int gridSize = 5, int rotationDegrees = 0) : Guide
{
    internal int HalfWidth = halfWidth;
    internal int HalfDepth = halfDepth;
    internal int GridSize = gridSize;
    internal int RotationDegrees = rotationDegrees;
    internal float RotationRadians => RotationDegrees * MathF.PI / 180f;

    public override void Draw(PctDrawList drawList)
    {
        drawList.AddText(North + Vector3.UnitY * 0.1f, 0xFFFFFFFF, "N", 5f);

        drawList.PathLineTo(NorthEast);
        drawList.PathLineTo(SouthEast);
        drawList.PathLineTo(SouthWest);
        drawList.PathLineTo(NorthWest);
        drawList.PathStroke(0xFFFFFFFF, PctStrokeFlags.Closed);


        drawList.PathLineTo(North);
        drawList.PathLineTo(South);
        drawList.PathStroke(0xFFFFFFFF);

        drawList.PathLineTo(East);
        drawList.PathLineTo(West);
        drawList.PathStroke(0xFFFFFFFF);

        for (var x = GridSize; x < HalfWidth; x += GridSize)
        {
            drawList.PathLineTo(PointAtOffset(x, HalfDepth));
            drawList.PathLineTo(PointAtOffset(x, -HalfDepth));
            drawList.PathStroke(0xCCFFFFFF, thickness: 1);
            drawList.PathLineTo(PointAtOffset(-x, HalfDepth));
            drawList.PathLineTo(PointAtOffset(-x, -HalfDepth));
            drawList.PathStroke(0xCCFFFFFF, thickness: 1);
        }
        for (var z = GridSize; z < HalfDepth; z += GridSize)
        {
            drawList.PathLineTo(PointAtOffset(HalfWidth, z));
            drawList.PathLineTo(PointAtOffset(-HalfWidth, z));
            drawList.PathStroke(0xCCFFFFFF, thickness: 1);
            drawList.PathLineTo(PointAtOffset(HalfWidth, -z));
            drawList.PathLineTo(PointAtOffset(-HalfWidth, -z));
            drawList.PathStroke(0xCCFFFFFF, thickness: 1);
        }
    }

    private Vector3 PointAtOffset(float x, float z)
    {
        var radius = new Vector3(x, 0, z).Length();
        var radians = MathF.Atan2(z, x);
        Vector3 offset = new(MathF.Cos(RotationRadians - radians), 0, MathF.Sin(RotationRadians - radians));
        return center + offset * radius;
    }

    public override Vector3 North => PointAtOffset(0, HalfDepth);
    public override Vector3 NorthEast => PointAtOffset(HalfWidth, HalfDepth);
    public override Vector3 East => PointAtOffset(HalfWidth, 0);
    public override Vector3 SouthEast => PointAtOffset(HalfWidth, -HalfDepth);
    public override Vector3 South => PointAtOffset(0, -HalfDepth);
    public override Vector3 SouthWest => PointAtOffset(-HalfWidth, -HalfDepth);
    public override Vector3 West => PointAtOffset(-HalfWidth, 0);
    public override Vector3 NorthWest => PointAtOffset(-HalfWidth, HalfDepth);
}