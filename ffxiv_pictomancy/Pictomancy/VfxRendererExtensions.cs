using Pictomancy.DXDraw;
using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Pictomancy;
public static class VfxRendererExtensions
{
    const float DefaultHeight = 7;

    const string OmenCircle = "general_1bf";
    const string OmenLine = "general02f";
    const string OmenRectangle = "general_x02f";

    public static string? GetOmenConeForAngle(int angle)
    {
        return angle switch
        {
            15 => "gl_fan015_0x",
            20 => "gl_fan020_0f",
            30 => "gl_fan030_1bf",
            45 => "gl_fan045_1bf",
            60 => "gl_fan060_1bf",
            80 => "gl_fan80_o0g",
            90 => "gl_fan090_1bf",
            120 => "gl_fan120_1bf",
            130 => "gl_fan130_0x",
            135 => "gl_fan135_c0g",
            150 => "gl_fan150_1bf",
            180 => "gl_fan180_1bf",
            210 => "gl_fan210_1bf",
            225 => "gl_fan225_c0g",
            270 => "gl_fan270_0100af",
            360 => OmenCircle,
            _ => null,
        };
    }

    public static readonly ImmutableList<(string, float)> OmenDonutHoleSizes = [
        ("gl_sircle_5003bf", 0.07f),
        ("gl_sircle_7006x", 0.086f),
        ("gl_sircle_1034bf", 0.1138f),
        ("gl_sircle_4005bf", 0.126f),
        ("gl_circle_5007_x1", 0.14f),
        ("gl_sircle_6009at", 1.5f),
        ("gl_sircle_6010bf", 0.166f),
        ("gl_sircle_1703x", 0.1767f),
        ("gl_sircle_2004bv", 0.2f),
        ("gl_sircle_7015k1", 0.215f),
        ("gl_sircle_3007bx", 0.233f),
        ("gl_sircle_2005bf", 0.25f),
        ("gl_sircle_1905bf", 0.263f),
        ("gl_sircle_3008bf", 0.266f),
        ("gl_sircle_1805r1", 0.277f),
        ("gl_sircle_4012c", 0.3f),
        ("x6r8_b4_donut13m_4_01k1", 0.311f),
        ("gl_sircle_2508_o0t1", 0.32f),
        ("x6fa_donut01_o0a1", 0.333f),
        ("gl_sircle_1505bt1", 0.335f),
        ("gl_sircle_1907y0x", 0.37f),
        ("gl_sircle_2008bi", 0.4f),
        ("gl_sircle_3014bf", 0.466f),
        ("gl_sircle_2010bf", 0.5f),
        ("gl_sircle_4021x", 0.525f),
        ("gl_sircle_2011v", 0.55f),
        ("gl_sircle_3520x", 0.572f),
        ("gl_sircle_5030c", 0.6f), // makes noise; replace if another 6 is found
        ("gl_sircle_1610_o0v", 0.625f),
        ("gl_sircle_1510bx", 0.666f),
        ("gl_sircle_1710_o0p", 0.71f),
        ("gl_sircle_2316_o0p", 0.733f),
        ("gl_sircle_2015bx", 0.75f),
        ("gl_sircle_1109w", 0.82f),
        ("gl_sircle_1715w", 0.886f),
        ("gl_sircle_2018w", 0.9f),
     ];

    public static string? GetDonutOmenForRadius(float innerRadius, float outerRadius)
    {
        var desiredRatio = innerRadius / outerRadius;
        var x = OmenDonutHoleSizes.Where(x =>
        {
            var comparison = desiredRatio - x.Item2;

            return comparison >= 0 && comparison < 0.01f;
        }).FirstOrDefault();
        return x.Item1;
    }

    public static (string, float) GetDonutHoleOmenForRadius(float innerRadius)
    {
        var (omen, actualInnerRadius) = OmenDonutHoleSizes[0];
        return (omen, innerRadius / actualInnerRadius);
    }

    public static void AddLine(this VfxRenderer r, string id, Vector3 start, Vector3 stop, float halfWidth, Vector4? color = null)
    {
        float rotation = MathF.Atan2(stop.X - start.X, stop.Z - start.Z);
        float length = Vector2.Distance(new Vector2(stop.X, stop.Z), new Vector2(start.X, start.Z));
        AddLine(r, id, start, length, halfWidth, rotation, color);
    }

    public static void AddLine(this VfxRenderer r, string id, Vector3 start, float length, float halfWidth, float rotation, Vector4? color = null)
    {
        r.AddOmen(id, OmenLine, start, new(halfWidth, DefaultHeight, length), rotation, color);
    }

    public static void AddRectangle(this VfxRenderer r, string id, Vector3 origin, float halfWidth, float halfLength, float rotation = 0, Vector4? color = null)
    {
        r.AddOmen(id, OmenRectangle, origin, new(halfWidth, DefaultHeight, halfLength), rotation, color);
    }

    public static void AddCircle(this VfxRenderer r, string id, Vector3 origin, float radius, Vector4? color = null)
    {
        r.AddOmen(id, OmenCircle, origin, new(radius, DefaultHeight, radius), 0, color);
    }

    public static bool AddCone(this VfxRenderer r, string id, Vector3 origin, float radius, float rotation, int angleWidth, Vector4? color = null)
    {
        var omen = GetOmenConeForAngle(angleWidth);
        if (omen != null)
            r.AddOmen(id, omen, origin, new(radius, DefaultHeight, radius), rotation, color);
        return omen != null;
    }

    // Add a donut with a specified inner radius. No outer radius.
    public static void AddDonutHole(this VfxRenderer r, string id, Vector3 origin, float innerRadius, Vector4? color = null)
    {
        var (omen, scale) = GetDonutHoleOmenForRadius(innerRadius);
        r.AddOmen(id, omen, origin, new(scale, DefaultHeight, scale), 0, color);
    }

    public static bool AddDonut(this VfxRenderer r, string id, Vector3 origin, float innerRadius, float outerRadius, Vector4? color = null)
    {
        var omen = GetDonutOmenForRadius(innerRadius, outerRadius);
        if (omen != null)
            r.AddOmen(id, omen, origin, new(outerRadius, DefaultHeight, outerRadius), 0, color);
        return omen != null;
    }

    public static bool AddFan(this VfxRenderer r, string id, Vector3 origin, float innerRadius, float outerRadius, float minAngle, float maxAngle, Vector4? color = null)
    {
        bool isCircle = maxAngle - minAngle >= 2 * MathF.PI - 0.000001;
        if (innerRadius <= 0)
        {
            if (isCircle)
            {
                r.AddCircle(id, origin, outerRadius, color);
                return true;
            }
            else
            {
                float angle = maxAngle - minAngle;
                float rotation = minAngle + angle / 2;
                return r.AddCone(id, origin, outerRadius, -rotation, (int)MathF.Round(angle / (MathF.PI * 2) * 360, 0), color);
            }
        }
        else
        {
            if (isCircle)
            {
                if (innerRadius > 1 && outerRadius > 50)
                {
                    r.AddDonutHole(id, origin, innerRadius, color);
                    return true;
                }
                else
                    return r.AddDonut(id, origin, innerRadius, outerRadius, color);
            }
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 AlphaDXToVFX(this uint color)
    {
        var colorVec = color.ToVector4();
        if (colorVec.W <= 0.31372549019f)
            colorVec.W *= 3.1875f;
        else
        {
            var remainder = colorVec.W - 0.31372549019f;
            colorVec.W = 1 + remainder * 1.96f;
        }
        return colorVec;
    }
}
