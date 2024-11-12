using ImGuiNET;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Pictomancy.DXDraw;
internal static class ColorExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ToUint(this Vector4 color)
    {
        return ImGui.ColorConvertFloat4ToU32(color);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 ToVector4(this uint color)
    {
        return ImGui.ColorConvertU32ToFloat4(color);
    }
}
