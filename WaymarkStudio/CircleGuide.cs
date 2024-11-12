using Pictomancy;
using System;
using System.Numerics;

namespace WaymarkStudio;
internal struct CircleGuide(int radius = 1, int spokes = 0, int rings = 1, int rotationDegrees = 0)
{
    internal Vector3 center;
    internal int Radius = radius;
    internal int Spokes = spokes;
    internal int Rings = rings;
    internal int RotationDegrees = rotationDegrees;
    internal readonly float RotationRadians => (-90 + RotationDegrees) * MathF.PI / 180f;

    internal void Draw(PctDrawList drawList)
    {
        float radiusStep = (float)Radius / Rings;
        for (int i = 1; i <= Rings; i++)
        {
            drawList.AddCircle(
                center,
                radiusStep * i,
                0xFFFFFFFF);
        }
        drawList.AddText(North + Vector3.UnitY * 0.1f, 0xFFFFFFFF, "N", 5f);
        if (Spokes > 0)
        {
            float angleStep = MathF.PI * 2 / Spokes;
            for (int step = 0; step < Spokes; step++)
            {
                float angle = RotationRadians + step * angleStep;
                Vector3 offset = new(MathF.Cos(angle), 0, MathF.Sin(angle));
                drawList.PathLineTo(center);
                drawList.PathLineTo(center + offset * Radius);
                drawList.PathStroke(0xFFFFFFFF, new());
            }
        }
    }

    private Vector3 PointAtAngle(int degrees)
    {
        var radians = degrees * MathF.PI / 180f;
        Vector3 offset = new(MathF.Cos(RotationRadians - radians), 0, MathF.Sin(RotationRadians - radians));
        return center + offset * Radius;
    }

    internal Vector3 North => PointAtAngle(0);
    internal Vector3 NorthEast => PointAtAngle(-45);
    internal Vector3 East => PointAtAngle(-90);
    internal Vector3 SouthEast => PointAtAngle(-135);
    internal Vector3 South => PointAtAngle(-180);
    internal Vector3 SouthWest => PointAtAngle(-225);
    internal Vector3 West => PointAtAngle(-270);
    internal Vector3 NorthWest => PointAtAngle(-315);
}
