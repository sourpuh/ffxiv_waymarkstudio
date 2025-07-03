namespace WaymarkStudio;
public enum Waymark
{
    A, B, C, D,
    One, Two, Three, Four,
}

internal static class Waymarks
{
    internal const float CircleRadius = 1.25f;
    internal const float SquareHalfWidth = 1.1f;
    internal const float SquareCornerRadius = 1.55563491861f;

    private static readonly (string name, uint iconId, uint color, uint glowColor, string vfxPath)[] Info =
    {
        ("A", 61241, 0xFF6E6EFF, 0xAA6E6EFF, "vfx/common/eff/fld_mark_a0f.avfx"),
        ("B", 61242, 0xFF9CFBF3, 0xAA9CFBF3, "vfx/common/eff/fld_mark_b0f.avfx"),
        ("C", 61243, 0xFFFEEC9B, 0xAAFEEC9B, "vfx/common/eff/fld_mark_c0f.avfx"),
        ("D", 61247, 0xFFFF78C8, 0xAAFF78C8, "vfx/common/eff/fld_mark_d0f.avfx"),
        ("1", 61244, 0xFF6E6EFF, 0xAA6E6EFF, "vfx/common/eff/fld_mark_10f.avfx"),
        ("2", 61245, 0xFF9CFBF3, 0xAA9CFBF3, "vfx/common/eff/fld_mark_20f.avfx"),
        ("3", 61246, 0xFFFEEC9B, 0xAAFEEC9B, "vfx/common/eff/fld_mark_30f.avfx"),
        ("4", 61248, 0xFFFF78C8, 0xAAFF78C8, "vfx/common/eff/fld_mark_40f.avfx"),
    };

    internal static uint GetIconId(Waymark w)
    {
        return Info[(int)w].iconId;
    }
    internal static string GetName(Waymark w)
    {
        return Info[(int)w].name;
    }
    internal static uint GetColor(Waymark w)
    {
        return Info[(int)w].color;
    }
    internal static uint GetGlowColor(Waymark w)
    {
        return Info[(int)w].glowColor;
    }
    internal static Waymark GetWaymark(string vfxPath)
    {
        switch (vfxPath)
        {
            case "vfx/common/eff/fld_mark_a0f.avfx":
                return Waymark.A;
            case "vfx/common/eff/fld_mark_b0f.avfx":
                return Waymark.B;
            case "vfx/common/eff/fld_mark_c0f.avfx":
                return Waymark.C;
            case "vfx/common/eff/fld_mark_d0f.avfx":
                return Waymark.D;
            case "vfx/common/eff/fld_mark_10f.avfx":
                return Waymark.One;
            case "vfx/common/eff/fld_mark_20f.avfx":
                return Waymark.Two;
            case "vfx/common/eff/fld_mark_30f.avfx":
                return Waymark.Three;
            case "vfx/common/eff/fld_mark_40f.avfx":
                return Waymark.Four;
        }
        return (Waymark)(-1);
    }
    internal static bool IsCircle(Waymark w)
    {
        return w is Waymark.A or Waymark.B or Waymark.C or Waymark.D;
    }
    internal static bool IsSquare(Waymark w)
    {
        return w is Waymark.One or Waymark.Two or Waymark.Three or Waymark.Four;
    }
}
