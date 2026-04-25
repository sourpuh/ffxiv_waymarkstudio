using System;
using System.Collections.Generic;
using System.Numerics;
using Transformation = Lumina.Data.Parsing.Common.Transformation;

namespace WaymarkStudio.Maps;
public class AABB
{
    internal Vector3 Min;
    internal Vector3 Max;
    internal Vector3 Center => (Min + Max) / 2;
    internal float LongAxisLength => MathF.Max((Max - Min).X, (Max - Min).Z);

    public static AABB BoundingPoints(IEnumerable<Vector3> points)
    {
        var min = float.MaxValue * Vector3.One;
        var max = float.MinValue * Vector3.One;
        foreach (var point in points)
        {
            min = Vector3.MinNumber(min, point);
            max = Vector3.MaxNumber(max, point);
        }
        return new AABB(min, max);
    }

    public AABB(Vector3 min, Vector3 max)
    {
        Min = min;
        Max = max;
    }

    public AABB(Transformation t)
    {
        Min = Convert(t.Translation) - Convert(t.Scale);
        Max = Convert(t.Translation) + Convert(t.Scale);
    }

    internal void RecenterXZ(Vector2 newCenter)
    {
        var dx = MathF.Max(newCenter.X - Min.X, Max.X - newCenter.X);
        var dz = MathF.Max(newCenter.Y - Min.Z, Max.Z - newCenter.Y);
        Min = new Vector3(newCenter.X - dx, Min.Y, newCenter.Y - dz);
        Max = new Vector3(newCenter.X + dx, Max.Y, newCenter.Y + dz);
    }

    public bool Contains(Vector3 v)
    {
        return v.X > Min.X &&
               v.Y > Min.Y &&
               v.Z > Min.Z &&
               v.X < Max.X &&
               v.Y < Max.Y &&
               v.Z < Max.Z;
    }

    private static Vector3 Convert(Lumina.Data.Parsing.Common.Vector3 v)
    {
        return new(v.X, v.Y, v.Z);
    }
}
