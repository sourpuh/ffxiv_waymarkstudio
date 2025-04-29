using SourOmen.Structs;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Pictomancy.VfxDraw;
internal static unsafe class VfxFunctions
{
    public delegate VfxData* CreateVfxDelegate(byte* path, VfxInitData* init, byte a3, byte a4, float originX, float originY, float originZ, float sizeX, float sizeY, float sizeZ, float angle, float duration, int a13);
    public static CreateVfxDelegate CreateVfxInternal;

    public delegate VfxInitData* VfxInitDataCtorDelegate(VfxInitData* self);
    public static VfxInitDataCtorDelegate VfxInitDataCtor;

    public delegate void DestroyVfxDataDelegate(VfxData* self);
    public static DestroyVfxDataDelegate DestroyVfx;

    public delegate long UpdateVfxTransformDelegate(VfxResourceInstance* vfxInstance, Matrix4x4* transform);
    public static UpdateVfxTransformDelegate UpdateVfxTransform;

    public delegate long UpdateVfxColorDelegate(VfxResourceInstance* vfxInstance, float r, float g, float b, float a);
    public static UpdateVfxColorDelegate UpdateVfxColor;

    public delegate void MatrixRotateDelegate(Matrix4x4* matrix, float rotation);
    public static MatrixRotateDelegate MatrixRotate;

    internal static void Initialize()
    {
        VfxInitDataCtor = Marshal.GetDelegateForFunctionPointer<VfxInitDataCtorDelegate>(PictoService.SigScanner.ScanText("E8 ?? ?? ?? ?? 8D 57 06 48 8D 4C 24 ??"));
        CreateVfxInternal = Marshal.GetDelegateForFunctionPointer<CreateVfxDelegate>(PictoService.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8B D8 48 8D 95"));
        DestroyVfx = Marshal.GetDelegateForFunctionPointer<DestroyVfxDataDelegate>(PictoService.SigScanner.ScanText("E8 ?? ?? ?? ?? 4D 89 A4 DE ?? ?? ?? ??"));
        UpdateVfxTransform = Marshal.GetDelegateForFunctionPointer<UpdateVfxTransformDelegate>(PictoService.SigScanner.ScanText("E8 ?? ?? ?? ?? EB 19 48 8B 0B"));
        UpdateVfxColor = Marshal.GetDelegateForFunctionPointer<UpdateVfxColorDelegate>(PictoService.SigScanner.ScanText("E8 ?? ?? ?? ?? 8B 4B F3"));
        MatrixRotate = Marshal.GetDelegateForFunctionPointer<MatrixRotateDelegate>(PictoService.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8D 77 20"));
    }
}
