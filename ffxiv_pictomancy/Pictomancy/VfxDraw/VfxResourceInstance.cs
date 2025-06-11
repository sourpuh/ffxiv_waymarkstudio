using System.Numerics;
using System.Runtime.InteropServices;

namespace SourOmen.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0xC0)]
public struct VfxResourceInstance
{
    [FieldOffset(0x90)] public Vector3 Scale;
    [FieldOffset(0xA0)] public Vector4 Color;
}
