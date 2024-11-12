namespace WaymarkStudio;
public enum Waymark
{
    A, B, C, D,
    One, Two, Three, Four,
}

internal static class Waymarks
{
    private static readonly (string name, uint iconId, uint color, uint glowColor)[] Info =
    {
        ("A", 61241, 0xFF6E6EFF, 0xAA6E6EFF),
        ("B", 61242, 0xFF9CFBF3, 0xAA9CFBF3),
        ("C", 61243, 0xFFFEEC9B, 0xAAFEEC9B),
        ("D", 61247, 0xFFFFACD2, 0xAAFFACD2),
        ("1", 61244, 0xFF6E6EFF, 0xAA6E6EFF),
        ("2", 61245, 0xFF9CFBF3, 0xAA9CFBF3),
        ("3", 61246, 0xFFFEEC9B, 0xAAFEEC9B),
        ("4", 61248, 0xFFFFACD2, 0xAAFFACD2),
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
}
