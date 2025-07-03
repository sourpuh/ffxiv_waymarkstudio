using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Utility;
using System;
using System.Numerics;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace WaymarkStudio.Maps;

using TexFile = Lumina.Data.Files.TexFile;

public class Map : IDisposable
{
    /// <summary>
    /// Full map Id; typically in the form "TerritoryTypeName/SubId".
    /// For example, two maps for the same zone are "x6r8/00" or "x6r8/01".
    /// </summary>
    public string Id { get; set; }

    private string BgTexPath => $"ui/map/{Id}/{Id.Replace("/", "")}m_m.tex";
    private string FgTexPath => $"ui/map/{Id}/{Id.Replace("/", "")}_m.tex";

    public string Name { get; set; }

    public ushort SizeFactor { get; set; }

    /// <summary>
    /// Scale of the map in pixels per world unit.
    /// </summary>
    public float Scale => SizeFactor / 100f;

    /// <summary>
    /// World position of the center of the map.
    /// </summary>
    public Vector2 Center { get; set; }

    /// <summary>
    /// Size of the map texture in pixels.
    /// </summary>
    public Vector2 SizePx = new(2048);

    private Task<IDalamudTextureWrap?>? textureLoadTask = null;
    /// <summary>
    /// Get the texture for this map or null if still loading / not found.
    /// </summary>
    public IDalamudTextureWrap? Texture
    {
        get
        {
            if (textureLoadTask == null)
                textureLoadTask = LoadTexture();
            else if (textureLoadTask.IsCompletedSuccessfully)
                return textureLoadTask.Result;
            return null;
        }
    }

    public Map(Lumina.Excel.Sheets.Map map)
    {
        Id = map.Id.ExtractText();
        Name = map.PlaceNameSub.Value.Name.ExtractText();
        SizeFactor = map.SizeFactor;
        Center = new Vector2(-map.OffsetX, -map.OffsetY);
    }

    public Vector2 NormTexToWorldCoords(Vector2 normalTexCoords)
    {
        normalTexCoords -= new Vector2(0.5f);
        var worldCoords = normalTexCoords * SizePx / Scale;
        return worldCoords + Center;
    }

    public Vector2 WorldToNormTexCoords(Vector2 worldCoords)
    {
        worldCoords -= Center;
        var normalTexCoords = worldCoords * Scale / SizePx;
        return normalTexCoords + new Vector2(0.5f);
    }

    public float WorldToNormTexScale => Scale / SizePx.X;

    public Vector2 WorldToNormTexCoords(Vector3 worldCoordinates)
    {
        return WorldToNormTexCoords(worldCoordinates.XZ());
    }

    private unsafe Task<IDalamudTextureWrap?> LoadTexture()
    {
        return Task.Run(() =>
        {
            var mapFg = Plugin.DataManager.GetFile<TexFile>(FgTexPath);
            if (mapFg == null)
                return null;

            var mapBg = Plugin.DataManager.GetFile<TexFile>(BgTexPath);
            var texture = BlendMapTextures(mapFg, mapBg);

            SizePx = texture.Size;
            return texture;
        });
    }

    private static IDalamudTextureWrap BlendMapTextures(TexFile mapFg, TexFile? mapBg)
    {
        var mapData = mapFg.GetRgbaImageData();
        if (mapBg != null)
        {
            var mapBgData = mapBg.GetRgbaImageData();
            for (var i = 0; i < mapData.Length; i++)
                mapData[i] = (byte)(mapData[i] * mapBgData[i] / 255f);
        }

        var width = mapFg.Header.Width;
        var height = mapFg.Header.Height;
        return Plugin.TextureProvider.CreateFromRaw(RawImageSpecification.Rgba32(width, height), mapData);
    }

    public void Dispose()
    {
        if (Texture != null)
            Texture.Dispose();
    }
}
