using System.Numerics;
using System.Runtime.InteropServices;

namespace Pictomancy.VfxDraw;

// UpdateVfxTransform uses an instruction that requires 16-byte aligned memory. Windows has 8-byte aligned heap so this fixes alignment.
internal unsafe class AlignedMatrix4x4 : IDisposable
{
    private nint rawptr;
    private nint aligned;
    public Matrix4x4* Get => (Matrix4x4*)aligned;

    public AlignedMatrix4x4()
    {
        rawptr = Marshal.AllocHGlobal(sizeof(Matrix4x4) + 8);
        aligned = new IntPtr(16 * (((long)rawptr + 15) / 16));
    }

    public void Dispose()
    {
        Marshal.FreeHGlobal(rawptr);
    }
}
