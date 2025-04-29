using System.Runtime.InteropServices;

namespace SourOmen.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x1E0)]
public struct VfxData
{
    [FieldOffset(0x1B8)] public unsafe VfxResourceInstance* Instance;
}
