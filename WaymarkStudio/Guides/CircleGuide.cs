using Pictomancy;
using System;
using System.Numerics;

namespace WaymarkStudio.Guides;
public class CircleGuide(float radius = 1, int spokes = 0, int rings = 1, int rotationDegrees = 0) : Guide
{
    internal float Radius = radius;
    internal int Spokes = spokes;
    internal int Rings = rings;
    internal int RotationDegrees = rotationDegrees;
    internal float RotationRadians => (-90 + RotationDegrees) * MathF.PI / 180f;

    public override void Draw(PctDrawList drawList)
    {
        var radiusStep = Radius / Rings;
        for (var i = 1; i <= Rings; i++)
        {
            drawList.AddCircle(
                center,
                radiusStep * i,
                i == Rings ? 0xFFFFFFFF : 0xCCFFFFFF,
                thickness: i == Rings ? 2 : 1);
        }
        drawList.AddText(North + Vector3.UnitY * 0.1f, 0xFFFFFFFF, "N", 5f);
        if (Spokes > 0)
        {
            var angleStep = MathF.PI * 2 / Spokes;
            for (var step = 0; step < Spokes; step++)
            {
                var angle = RotationRadians + step * angleStep;
                Vector3 offset = new(MathF.Cos(angle), 0, MathF.Sin(angle));
                drawList.PathLineTo(center);
                drawList.PathLineTo(center + offset * Radius);
                drawList.PathStroke(0xFFFFFFFF);
            }
        }
    }

    private Vector3 PointAtAngle(int degrees)
    {
        var radians = degrees * MathF.PI / 180f;
        Vector3 offset = new(MathF.Cos(RotationRadians - radians), 0, MathF.Sin(RotationRadians - radians));
        return center + offset * Radius;
    }

    public override Vector3 North => PointAtAngle(0);
    public override Vector3 NorthEast => PointAtAngle(-45);
    public override Vector3 East => PointAtAngle(-90);
    public override Vector3 SouthEast => PointAtAngle(-135);
    public override Vector3 South => PointAtAngle(-180);
    public override Vector3 SouthWest => PointAtAngle(-225);
    public override Vector3 West => PointAtAngle(-270);
    public override Vector3 NorthWest => PointAtAngle(-315);
}
