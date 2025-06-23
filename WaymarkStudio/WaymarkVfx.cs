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
            foreach ((var w, var vfx) in TrackedVfx)
            {
                vfx.UpdateColor(new(1, 1, 1, GetEffectiveAlpha(w)));
            }
        }
    }
    internal unsafe Dictionary<Waymark, Vfx> TrackedVfx = new();
    internal unsafe Dictionary<Waymark, float> VfxAlpha = new();

    public WaymarkVfx()
    {
        Plugin.Hooker.InitializeFromAttributes(this);
        CreateVfxHook.Enable();
        WaymarkAlpha = 1;
    }
    public void Dispose()
    {
        VfxAlpha.Clear();
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
            var waymark = Waymarks.GetWaymark(pathstr);
            TrackedVfx[waymark] = Vfx.Wrap(vfxData, position, size, angle);
            vfxData->Instance->Color = new(1, 1, 1, GetEffectiveAlpha(waymark));
        }
        return vfxData;
    }

    public bool IsAnyWaymarkHidden()
    {
        foreach ((var w, var vfx) in TrackedVfx)
        {
            if (GetEffectiveAlpha(w) == 0) return true;
        }
        return false;
    }

    public float? GetAlphaOverride(Waymark w)
    {
        if (VfxAlpha.TryGetValue(w, out float value))
            return value;
        return null;
    }

    public float GetEffectiveAlpha(Waymark w)
    {
        if (VfxAlpha.TryGetValue(w, out float value))
            return value;
        return WaymarkAlpha;
    }

    public void SetAlphaOverride(Waymark w, float? alpha)
    {
        if (alpha.HasValue)
            VfxAlpha[w] = (float)alpha;
        else
            VfxAlpha.Remove(w);
        if (TrackedVfx.TryGetValue(w, out var vfx))
            vfx.UpdateColor(new(1, 1, 1, GetEffectiveAlpha(w)));
    }

    public void ResetAlphaOverrides()
    {
        VfxAlpha.Clear();
    }
}
