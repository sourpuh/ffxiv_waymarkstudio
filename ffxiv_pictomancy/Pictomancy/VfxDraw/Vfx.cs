using SourOmen.Structs;
using System.Numerics;

namespace Pictomancy.VfxDraw;
public unsafe class Vfx : IDisposable
{
    internal Vector3 position;
    internal Vector3 size;
    internal float rotation;
    internal Vector4 color;
    internal VfxData* data;

    public static Vfx Create(string path, Vector3 position, Vector3 size, float rotation, Vector4? color = null)
    {
        return new(path, position, size, rotation, color ?? Vector4.One);
    }

    private Vfx(string path, Vector3 position, Vector3 size, float rotation, Vector4 color)
    {
        this.position = position;
        this.size = size;
        this.rotation = rotation;
        data = CreateVfxInternal(path, position, size, rotation, color);
    }

    public void Dispose()
    {
        VfxFunctions.DestroyVfx(data);
        data = null;
    }

    private static VfxData* CreateVfxInternal(string path, Vector3 position, Vector3 size, float rotation, Vector4? color = null)
    {
        var pathBytes = System.Text.Encoding.UTF8.GetBytes(path);

        var init = new VfxInitData();
        VfxFunctions.VfxInitDataCtor(&init);

        fixed (byte* pathPtr = pathBytes)
        {
            var vfx = VfxFunctions.CreateVfxInternal(pathPtr, &init, 2, 0, position.X, position.Y, position.Z, size.X, size.Y, size.Z, rotation, 1, -1);
            vfx->Instance->Color = color ?? Vector4.One;

            return vfx;
        }
    }

    public void UpdateTransform(Vector3 position, Vector3 size, float rotation)
    {
        if (this.position == position && this.rotation == rotation && this.size == size)
            return;

        this.position = position;
        this.size = size;
        this.rotation = rotation;

        AlignedMatrix4x4 matrix = new();
        Matrix4x4* alignedMatrix = matrix.Get;
        alignedMatrix->M11 = size.X;
        alignedMatrix->M12 = 0;
        alignedMatrix->M13 = 0;
        alignedMatrix->M14 = 0;

        alignedMatrix->M21 = 0;
        alignedMatrix->M22 = size.Y;
        alignedMatrix->M23 = 0;
        alignedMatrix->M24 = 0;

        alignedMatrix->M31 = 0;
        alignedMatrix->M32 = 0;
        alignedMatrix->M33 = size.Z;
        alignedMatrix->M34 = 0;

        VfxFunctions.MatrixRotate(alignedMatrix, rotation);

        alignedMatrix->M41 = position.X;
        alignedMatrix->M42 = position.Y;
        alignedMatrix->M43 = position.Z;
        alignedMatrix->M44 = 0;
        VfxFunctions.UpdateVfxTransform(data->Instance, alignedMatrix);
    }

    public void UpdateColor(Vector4 color)
    {
        if (this.color == color)
            return;

        this.color = color;
        VfxFunctions.UpdateVfxColor(data->Instance, color.X, color.Y, color.Z, color.W);
    }
}
