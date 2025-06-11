using Dalamud.Game.ClientState.Objects.Types;
using Pictomancy.VfxDraw;
using System.Numerics;

namespace Pictomancy;
public class VfxRenderer : IDisposable
{
    static readonly Vector4 White = Vector4.One;

    private static string OmenPath(string x)
    {
        return $"vfx/omen/eff/{x}.avfx";
    }

    private static string LockonPath(string x)
    {
        return $"vfx/lockon/eff/{x}.avfx";
    }

    private static string ChannelingPath(string x)
    {
        return $"vfx/channeling/eff/{x}.avfx";
    }

    private static string CommonPath(string x)
    {
        return $"vfx/common/eff/{x}.avfx";
    }

    InterframeResourceTracker<Vfx> trackedVfx;
    InterframeResourceTracker<GameObjectVfx> trackedGOVfx;

    public VfxRenderer()
    {
        trackedVfx = new();
        trackedGOVfx = new();
    }

    /// <summary>
    /// Add an omen VFX at a position with scale, rotation, and color.
    /// If the ID is specified in consecutive frames, the VFX is updated to match the new parameters.
    /// If the ID is not specified in consecutive frames, the VFX is destroyed.
    /// </summary>
    /// <param name="id">Unique ID used to track the VFX across frames</param>
    /// <param name="name">Name of the omen VFX (not the full path) "vfx/omen/eff/{name}.avfx"</param>
    /// <param name="origin">World position of the origin of the VFX</param>
    /// <param name="scale">Scale</param>
    /// <param name="rotation">Rotation</param>
    /// <param name="color">Color of the VFX; values beyond 1 are supported</param>
    public void AddOmen(string id, string name, Vector3 origin, Vector3? scale = null, float rotation = 0, Vector4? color = null)
    {
        CreateOrUpdateVfx(id, OmenPath(name), origin, scale ?? Vector3.One, rotation, color ?? White);
    }

    /// <summary>
    /// Add a lockon VFX to a target GameObject with scale and color.
    /// If the ID is specified in consecutive frames, the VFX is updated to match the new parameters.
    /// If the ID is not specified in consecutive frames, the VFX is destroyed.
    /// </summary>
    /// <param name="id">Unique ID used to track the VFX across frames</param>
    /// <param name="name">Name of the lockon VFX (not the full path) "vfx/lockon/eff/{name}.avfx"</param>
    /// <param name="target">Target GameObject to be the parent of the VFX</param>
    /// <param name="scale">Scale; not recommended</param>
    /// <param name="color">Color of the VFX; not recommended; values beyond 1 are supported</param>
    public void AddLockon(string id, string name, IGameObject target, Vector3? scale = null, Vector4? color = null)
    {
        CreateOrUpdateVfx(id, LockonPath(name), target, target, scale ?? Vector3.One, color ?? White);
    }

    /// <summary>
    /// Add a channeling VFX between a source and a target GameObjects with scale and color.
    /// If the ID is specified in consecutive frames, the VFX is updated to match the new parameters.
    /// If the ID is not specified in consecutive frames, the VFX is destroyed.
    /// </summary>
    /// <param name="id">Unique ID used to track the VFX across frames</param>
    /// <param name="name">Name of the channeling VFX (not the full path) "vfx/channeling/eff/{name}.avfx"</param>
    /// <param name="target">Target GameObject to be the end point of the VFX</param>
    /// <param name="source">Source GameObject to be the start point of the VFX</param>
    /// <param name="scale">Scale; not recommended</param>
    /// <param name="color">Color of the VFX; not recommended; values beyond 1 are supported</param>
    public void AddChanneling(string id, string name, IGameObject target, IGameObject source, Vector3? scale = null, Vector4? color = null)
    {
        CreateOrUpdateVfx(id, ChannelingPath(name), target, source, scale ?? Vector3.One, color ?? White);
    }

    /// <summary>
    /// Add a common VFX to a target GameObject with scale and color.
    /// If the ID is specified in consecutive frames, the VFX is updated to match the new parameters.
    /// If the ID is not specified in consecutive frames, the VFX is destroyed.
    /// </summary>
    /// <param name="id">Unique ID used to track the VFX across frames</param>
    /// <param name="name">Name of the common VFX (not the full path) "vfx/common/eff/{name}.avfx"</param>
    /// <param name="target">Target GameObject to be the parent of the VFX</param>
    /// <param name="scale">Scale; not recommended</param>
    /// <param name="color">Color of the VFX; not recommended; values beyond 1 are supported</param>
    public void AddCommon(string id, string name, IGameObject target, Vector3? scale = null, Vector4? color = null)
    {
        CreateOrUpdateVfx(id, CommonPath(name), target, target, scale ?? Vector3.One, color ?? White);
    }

    private void CreateOrUpdateVfx(string id, string path, Vector3 position, Vector3 scale, float rotation, Vector4 color)
    {
        var key = $"{id}##{path}";
        if (trackedVfx.IsTouched(key)) return;

        if (trackedVfx.TryTouchExisting(key, out Vfx vfx))
        {
            vfx.UpdateTransform(position, scale, rotation);
            vfx.UpdateColor(color);
        }
        else
        {
            trackedVfx.TouchNew(key, Vfx.Create(path, position, scale, rotation, color));
        }
    }

    private void CreateOrUpdateVfx(string id, string path, IGameObject target, IGameObject source, Vector3 scale, Vector4 color)
    {
        var key = $"{id}##{path}";
        if (trackedGOVfx.IsTouched(key)) return;

        if (trackedGOVfx.TryTouchExisting(key, out GameObjectVfx vfx))
        {
            vfx.UpdateColor(color);
        }
        else
        {
            trackedGOVfx.TouchNew(key, GameObjectVfx.Create(path, target, source, scale, color));
        }
    }

    internal void Update()
    {
        trackedVfx.Update();
        trackedGOVfx.Update();
    }

    public void Dispose()
    {
        trackedVfx.Dispose();
        trackedGOVfx.Dispose();
    }
}
