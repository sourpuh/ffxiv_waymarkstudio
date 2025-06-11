using SourOmen.Structs;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Pictomancy.VfxDraw;
public static unsafe class VfxFunctions
{
    public const string VfxInitDataCtorSig = "E8 ?? ?? ?? ?? 8D 57 06 48 8D 4C 24 ??";
    public delegate VfxInitData* VfxInitDataCtorDelegate(VfxInitData* self);
    public static VfxInitDataCtorDelegate VfxInitDataCtor;

    public const string CreateVfxSig = "E8 ?? ?? ?? ?? 48 8B D8 48 8D 95";
    public delegate VfxData* CreateVfxDelegate(byte* path, VfxInitData* init, byte a3, byte a4, float originX, float originY, float originZ, float sizeX, float sizeY, float sizeZ, float angle, float duration, int a13);
    public static CreateVfxDelegate CreateVfx;

    public const string CreateGameObjectVfxSig = "E8 ?? ?? ?? ?? 48 8B D8 48 85 C0 74 27 B2 01";
    public delegate VfxData* CreateGameObjectVfxDelegate(byte* path, nint target, nint source, float a4, byte a5, ushort a6, byte a7);
    public static CreateGameObjectVfxDelegate CreateGameObjectVfxInternal;

    public const string DestroyVfxSig = "E8 ?? ?? ?? ?? 4D 89 A4 DE ?? ?? ?? ??";
    public delegate void DestroyVfxDataDelegate(VfxData* self);
    public static DestroyVfxDataDelegate DestroyVfx;

    public const string UpdateVfxTransformSig = "E8 ?? ?? ?? ?? EB 19 48 8B 0B";
    public delegate long UpdateVfxTransformDelegate(VfxResourceInstance* vfxInstance, Matrix4x4* transform);
    public static UpdateVfxTransformDelegate UpdateVfxTransform;

    public const string UpdateVfxColorSig = "E8 ?? ?? ?? ?? 8B 4B F3";
    public delegate long UpdateVfxColorDelegate(VfxResourceInstance* vfxInstance, float r, float g, float b, float a);
    public static UpdateVfxColorDelegate UpdateVfxColor;

    public const string RotateMatrixSig = "E8 ?? ?? ?? ?? 48 8D 77 20";
    public delegate void RotateMatrixDelegate(Matrix4x4* matrix, float rotation);
    public static RotateMatrixDelegate RotateMatrix;

    internal static void Initialize()
    {
        VfxInitDataCtor = Marshal.GetDelegateForFunctionPointer<VfxInitDataCtorDelegate>(PictoService.SigScanner.ScanText(VfxInitDataCtorSig));
        CreateVfx = Marshal.GetDelegateForFunctionPointer<CreateVfxDelegate>(PictoService.SigScanner.ScanText(CreateVfxSig));
        CreateGameObjectVfxInternal = Marshal.GetDelegateForFunctionPointer<CreateGameObjectVfxDelegate>(PictoService.SigScanner.ScanText(CreateGameObjectVfxSig));
        DestroyVfx = Marshal.GetDelegateForFunctionPointer<DestroyVfxDataDelegate>(PictoService.SigScanner.ScanText(DestroyVfxSig));
        UpdateVfxTransform = Marshal.GetDelegateForFunctionPointer<UpdateVfxTransformDelegate>(PictoService.SigScanner.ScanText(UpdateVfxTransformSig));
        UpdateVfxColor = Marshal.GetDelegateForFunctionPointer<UpdateVfxColorDelegate>(PictoService.SigScanner.ScanText(UpdateVfxColorSig));
        RotateMatrix = Marshal.GetDelegateForFunctionPointer<RotateMatrixDelegate>(PictoService.SigScanner.ScanText(RotateMatrixSig));
    }
}
