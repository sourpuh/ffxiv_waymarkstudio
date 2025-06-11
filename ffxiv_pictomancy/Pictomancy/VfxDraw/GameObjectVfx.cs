using Dalamud.Game.ClientState.Objects.Types;
using SourOmen.Structs;
using System.Numerics;

namespace Pictomancy.VfxDraw;
public unsafe class GameObjectVfx : IDisposable
{
    internal IGameObject target;
    internal IGameObject source;
    internal Vector3 scale;
    internal Vector4 color;
    internal VfxData* data;

    public static GameObjectVfx Create(string path, IGameObject target, IGameObject source, Vector3 scale, Vector4 color)
    {
        return new(path, target, source, scale, color);
    }

    private GameObjectVfx(string path, IGameObject target, IGameObject source, Vector3 scale, Vector4 color)
    {
        this.target = target;
        this.source = source;
        this.scale = scale;
        this.color = color;
        data = CreateGameObjectVfxInternal(path, target, source, scale, color);
    }

    public void Dispose()
    {
        VfxFunctions.DestroyVfx(data);
        data = null;
    }

    private static VfxData* CreateGameObjectVfxInternal(string path, IGameObject target, IGameObject source, Vector3 scale, Vector4 color)
    {
        var pathBytes = System.Text.Encoding.UTF8.GetBytes(path);
        fixed (byte* pathPtr = pathBytes)
        {
            var vfx = VfxFunctions.CreateGameObjectVfxInternal(pathPtr, target.Address, source.Address, 1, 0, 0, 1);
            vfx->Instance->Scale = scale;
            vfx->Instance->Color = color;

            return vfx;
        }
    }
    public void UpdateColor(Vector4 color)
    {
        if (this.color == color)
            return;

        this.color = color;
        VfxFunctions.UpdateVfxColor(data->Instance, color.X, color.Y, color.Z, color.W);
    }
}
