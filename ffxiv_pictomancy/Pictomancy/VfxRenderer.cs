using Pictomancy.DXDraw;
using Pictomancy.VfxDraw;
using System.Numerics;

namespace Pictomancy;
public class VfxRenderer : IDisposable
{
    const float DefaultHeight = 7;
    const uint White = 0xFFFFFFFF;
    //const string OmenCircle = "general01bf"; // This is a nice white color that could be used for something
    const string OmenCircle = "general_1bf";
    const string OmenLine = "general02f";
    const string OmenRectangle = "general_x02f";
    const string OmenFan15 = "gl_fan015_0x";
    const string OmenFan20 = "gl_fan020_0f";
    const string OmenFan30 = "gl_fan030_1bf";
    const string OmenFan45 = "gl_fan045_1bf";
    const string OmenFan60 = "gl_fan060_1bf";
    const string OmenFan80 = "gl_fan80_o0g";
    const string OmenFan90 = "gl_fan090_1bf";
    //const string OmenFan100 = "er_gl_fan100_o0v"; // Too dark
    const string OmenFan120 = "gl_fan120_1bf";
    const string OmenFan130 = "gl_fan130_0x";
    const string OmenFan135 = "gl_fan135_c0g";
    //const string OmenFan145 = "m0501_fan145_d1"; // Too dark and also scaled weird
    const string OmenFan150 = "gl_fan150_1bf";
    const string OmenFan180 = "gl_fan180_1bf";
    const string OmenFan210 = "gl_fan210_1bf";
    const string OmenFan225 = "gl_fan225_c0g";
    const string OmenFan270 = "gl_fan270_0100af";

    const string OmenDonut0_7 = "gl_sircle_5003bf";
    const string OmenDonut0_9 = "gl_sircle_7006x";
    const string OmenDonut1_11 = "gl_sircle_1034bf";
    const string OmenDonut1_5 = "gl_circle_5007_x1";
    const string OmenDonut1_66 = "gl_sircle_6010bf";
    const string OmenDonut1_8 = "gl_sircle_1703x";
    const string OmenDonut2 = "gl_sircle_2004bv";
    const string OmenDonut2_15 = "gl_sircle_7015k1";
    const string OmenDonut2_33 = "gl_sircle_3007bx";
    const string OmenDonut2_5 = "gl_sircle_2005bf";
    const string OmenDonut2_62 = "gl_sircle_1905bf";
    const string OmenDonut2_66 = "gl_sircle_3008bf";
    const string OmenDonut2_8 = "gl_sircle_1805r1";
    const string OmenDonut3 = "gl_sircle_4012c";
    const string OmenDonut3_11 = "x6r8_b4_donut13m_4_01k1";
    const string OmenDonut3_2 = "gl_sircle_2508_o0t1";
    const string OmenDonut3_33 = "x6r8_b4_donut13m_4_01k1";
    const string OmenDonut3_7 = "gl_sircle_1907y0x";
    const string OmenDonut4 = "gl_sircle_2008bi";
    const string OmenDonut4_66 = "gl_sircle_3014bf";
    const string OmenDonut5 = "gl_sircle_2010bf";
    const string OmenDonut5_5 = "gl_sircle_2011v";
    const string OmenDonut6 = "gl_sircle_5030c"; // makes noise; replace if another 6 is found
    const string OmenDonut6_33 = "gl_sircle_1610_o0v";
    const string OmenDonut6_66 = "gl_sircle_1510bx";
    const string OmenDonut7_1 = "gl_sircle_1710_o0p";
    const string OmenDonut7_33 = "gl_sircle_2316_o0p";
    const string OmenDonut7_5 = "gl_sircle_2015bx";
    const string OmenDonut8_2 = "gl_sircle_1109w";
    const string OmenDonut8_9 = "gl_sircle_1715w";
    const string OmenDonut9 = "gl_sircle_2018w";

    List<(string, float)> OmenDonutHoleSize = [
        (OmenDonut0_7, 0.07f),
        (OmenDonut0_9, 0.09f),
        (OmenDonut1_11, 0.111f),
        (OmenDonut1_5, 0.15f),
        (OmenDonut1_66, 0.166f),
        (OmenDonut1_8, 0.18f),
        (OmenDonut2, 0.2f),
        (OmenDonut2_15, 0.215f),
        (OmenDonut2_33, 0.233f),
        (OmenDonut2_5, 0.25f),
        (OmenDonut2_62, 0.262f),
        (OmenDonut2_66, 0.266f),
        (OmenDonut2_8, 0.28f),
        (OmenDonut3, 0.3f),
        (OmenDonut3_11, 0.311f),
        (OmenDonut3_2, 0.32f),
        (OmenDonut3_33, 0.333f),
        (OmenDonut3_7, 0.37f),
        (OmenDonut4, 0.4f),
        (OmenDonut4_66, 0.466f),
        (OmenDonut5, 0.5f),
        (OmenDonut5_5, 0.55f),
        (OmenDonut6, 0.6f),
        (OmenDonut6_33, 0.633f),
        (OmenDonut6_66, 0.666f),
        (OmenDonut7_1, 0.71f),
        (OmenDonut7_33, 0.733f),
        (OmenDonut7_5, 0.75f),
        (OmenDonut8_2, 0.82f),
        (OmenDonut8_9, 0.89f),
        (OmenDonut9, 0.9f),
     ];

    private string? GetOmenConeForAngle(int angleWidth)
    {
        return angleWidth switch
        {
            15 => OmenFan15,
            20 => OmenFan20,
            30 => OmenFan30,
            45 => OmenFan45,
            60 => OmenFan60,
            80 => OmenFan80,
            90 => OmenFan90,
            // 100 => OmenFan100,
            120 => OmenFan120,
            130 => OmenFan130,
            135 => OmenFan135,
            // 145 => OmenFan145,
            150 => OmenFan150,
            180 => OmenFan180,
            210 => OmenFan210,
            225 => OmenFan225,
            270 => OmenFan270,
            360 => OmenCircle,
            _ => null,
        };
    }

    private string? GetDonutOmen(float innerRadius, float outerRadius)
    {
        var desiredRatio = innerRadius / outerRadius;
        var x = OmenDonutHoleSize.Where(x =>
        {
            var comparison = desiredRatio - x.Item2;

            return comparison >= 0 && comparison < 0.01f;
        }).FirstOrDefault();
        return x.Item1;
    }

    private (string, float) GetDonutHoleOmen(float innerRadius)
    {
        var (omen, actualInnerRadius) = OmenDonutHoleSize[0];
        return (omen, innerRadius / actualInnerRadius);
    }

    private static string OmenPath(string x)
    {
        return $"vfx/omen/eff/{x}.avfx";
    }

    private Dictionary<string, Vfx> prevActiveVfx;
    private Dictionary<string, Vfx> currActiveVfx;

    public VfxRenderer()
    {
        prevActiveVfx = new();
        currActiveVfx = new();
    }

    private void CreateOrUpdateVfx(string id, string vfxname, Vector3 position, Vector3 size, float rotation, Vector4 color)
    {
        var key = $"{id}##{vfxname}";
        if (currActiveVfx.ContainsKey(key)) return;

        Vfx vfx;
        if (prevActiveVfx.TryGetValue(key, out vfx))
        {
            vfx.UpdateTransform(position, size, rotation);
            vfx.UpdateColor(color);
            prevActiveVfx.Remove(key);
        }
        else
        {
            vfx = Vfx.Create(OmenPath(vfxname), position, size, rotation, color);
        }
        currActiveVfx.Add(key, vfx);
    }

    public void AddLine(string id, Vector3 start, Vector3 stop, float halfWidth, uint color = White)
    {
        float rotation = MathF.Atan2(stop.X - start.X, stop.Z - start.Z);
        float length = Vector2.Distance(new Vector2(stop.X, stop.Z), new Vector2(start.X, start.Z));
        AddLine(id, start, length, halfWidth, rotation, color);
    }

    public void AddLine(string id, Vector3 start, float length, float halfWidth, float rotation, uint color = White)
    {
        CreateOrUpdateVfx(id, OmenLine, start, new(halfWidth, DefaultHeight, length), rotation, color.ToVector4());
    }

    public void AddRectangle(string id, Vector3 origin, float halfWidth, float halfLength, float rotation = 0, uint color = White)
    {
        CreateOrUpdateVfx(id, OmenRectangle, origin, new(halfWidth, DefaultHeight, halfLength), rotation, color.ToVector4());
    }

    public void AddCircle(string id, Vector3 origin, float radius, uint color = White)
    {
        CreateOrUpdateVfx(id, OmenCircle, origin, new(radius, DefaultHeight, radius), 0, color.ToVector4());
    }

    public bool AddCone(string id, Vector3 origin, float radius, float rotation, int angleWidth, uint color = White)
    {
        var omen = GetOmenConeForAngle(angleWidth);
        if (omen != null)
        {
            CreateOrUpdateVfx(id, omen, origin, new(radius, DefaultHeight, radius), rotation, color.ToVector4());
        }
        return omen != null;
    }

    /**
     * Add a donut with a specified inner radius. No outer radius.
     */
    public void AddDonutHole(string id, Vector3 origin, float innerRadius, uint color = White)
    {
        var (omen, scale) = GetDonutHoleOmen(innerRadius);
        CreateOrUpdateVfx(id, omen, origin, new(scale, DefaultHeight, scale), 0, color.ToVector4());
    }

    public bool AddDonut(string id, Vector3 origin, float innerRadius, float outerRadius, uint color = White)
    {
        var omen = GetDonutOmen(innerRadius, outerRadius);
        if (omen != null)
            CreateOrUpdateVfx(id, omen, origin, new(outerRadius, DefaultHeight, outerRadius), 0, color.ToVector4());
        return omen != null;
    }

    public void AddCustomOmen(string id, string name, Vector3 origin, Vector3 size, float rotation, uint color = White)
    {
        CreateOrUpdateVfx(id, name, origin, size, rotation, color.ToVector4());
    }

    internal void Update()
    {
        foreach (var item in prevActiveVfx)
        {
            item.Value.Dispose();
        }
        prevActiveVfx.Clear();

        var tmp = prevActiveVfx;
        prevActiveVfx = currActiveVfx;
        currActiveVfx = tmp;
    }

    public void Dispose()
    {
        foreach (var item in prevActiveVfx)
        {
            item.Value.Dispose();
        }
        foreach (var item in currActiveVfx)
        {
            item.Value.Dispose();
        }
    }
}
