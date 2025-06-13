using Dalamud.Hooking;
using Dalamud.Memory;
using Dalamud.Utility.Signatures;
using Pictomancy.VfxDraw;
using SourOmen.Structs;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace WaymarkStudio;
internal class WaymarkVfx : IDisposable
{
    [Signature(VfxFunctions.CreateVfxSig, DetourName = nameof(CreateVfxDetour))]
    private Hook<VfxFunctions.CreateVfxDelegate> CreateVfxHook = null!;

    internal float WaymarkAlpha
    {
        get;
        set
        {
            field = value;
            foreach (var vfx in TrackedVfx.Values)
            {
                vfx.UpdateColor(new(1, 1, 1, field));
            }
        }
    }
    internal unsafe Dictionary<Waymark, Vfx> TrackedVfx = new();

    public WaymarkVfx()
    {
        Plugin.Hooker.InitializeFromAttributes(this);
        CreateVfxHook.Enable();
        WaymarkAlpha = 1;
    }
    public void Dispose()
    {
        WaymarkAlpha = 1;
        CreateVfxHook.Dispose();
    }

    public unsafe void Update()
    {
        foreach (Waymark w in Enum.GetValues<Waymark>())
        {
            if (TrackedVfx.TryGetValue(w, out var vfx) && !vfx.IsValid)
                TrackedVfx.Remove(w);
        }
    }

    private unsafe VfxData* CreateVfxDetour(byte* path, VfxInitData* init, byte a3, byte a4, float originX, float originY, float originZ, float sizeX, float sizeY, float sizeZ, float angle, float duration, int a13)
    {
        var vfxData = CreateVfxHook.Original(path, init, a3, a4, originX, originY, originZ, sizeX, sizeY, sizeZ, angle, duration, a13);

        var pathstr = MemoryHelper.ReadSeStringNullTerminated((nint)path).ToString();
        if (pathstr.StartsWith("vfx/common/eff/fld_mark_"))
        {
            Vector3 position = new(originX, originY, originZ);
            Vector3 size = new(sizeX, sizeY, sizeZ);
            TrackedVfx[Waymarks.GetWaymark(pathstr)] = Vfx.Wrap(vfxData, position, size, angle);
            vfxData->Instance->Color = new(1, 1, 1, WaymarkAlpha);
        }
        return vfxData;
    }
}
