using Pictomancy;
using System;
using System.Collections.Generic;
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
        for (var ring = 1; ring <= Rings; ring++)
        {
            drawList.AddCircle(
                center,
                radiusStep * ring,
                ring == Rings ? 0xFFFFFFFF : 0xCCFFFFFF,
                thickness: ring == Rings ? 2 : 1);
        }
        drawList.AddText(PointAtDegrees(0, Radius + 0.1f) + Vector3.UnitY * 0.1f, 0xFFFFFFFF, "N", 1.5f);
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

        foreach (var point in SnapPoints)
        {
            drawList.AddDot(point, 2, 0xFFFFFFFF);
        }
    }

    private Vector3 PointAtDegrees(int degrees, float radius)
    {
        var radians = degrees * MathF.PI / 180f;
        return PointAtRadians(radians, radius);
    }

    private Vector3 PointAtRadians(float radians, float radius)
    {
        Vector3 offset = new(MathF.Cos(RotationRadians - radians), 0, MathF.Sin(RotationRadians - radians));
        return center + offset * radius;
    }

    public override Vector3 North => PointAtDegrees(0, Radius);
    public override Vector3 NorthEast => PointAtDegrees(-45, Radius);
    public override Vector3 East => PointAtDegrees(-90, Radius);
    public override Vector3 SouthEast => PointAtDegrees(-135, Radius);
    public override Vector3 South => PointAtDegrees(-180, Radius);
    public override Vector3 SouthWest => PointAtDegrees(-225, Radius);
    public override Vector3 West => PointAtDegrees(-270, Radius);
    public override Vector3 NorthWest => PointAtDegrees(-315, Radius);

    public override IEnumerable<Vector3> SnapPoints
    {
        get
        {
            List<Vector3> points = new();
            points.Add(center);

            var spokes = Spokes > 0 ? Spokes : 8;

            var angleStep = MathF.PI * 2 / spokes;
            for (var step = 0; step < spokes; step++)
            {
                var radiusStep = Radius / Rings;
                for (var ring = 1; ring <= Rings; ring++)
                {
                    points.Add(PointAtRadians(step * angleStep, ring * radiusStep));
                }
            }
            return points;
        }
    }
}
