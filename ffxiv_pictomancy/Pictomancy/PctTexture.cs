using System.Numerics;

namespace Pictomancy;
public record struct PctTexture(nint textureId, uint width, uint height)
{
    public nint TextureId = textureId;
    public uint Width = width;
    public uint Height = height;
    public Vector2 Size => new(Width, Height);
}
