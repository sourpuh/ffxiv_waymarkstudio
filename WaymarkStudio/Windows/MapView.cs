using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Collections.Generic;
using System.Numerics;
using WaymarkStudio.Maps;

namespace WaymarkStudio.Windows;

internal partial class MapView
{
    private static readonly Vector2 WaymarkMapIconHalfSizePx = new(15);
    public Map map;
    public Vector2 sizePx;
    /// <summary>
    /// Normalized texture coord pan. (0.5, 0.5) is centered.
    /// </summary>
    public Vector2 pan;
    /// <summary>
    /// Magnification level. 1 = 1x; 2 = 2x; etc.
    /// </summary>
    public float zoom;
    public Vector2 windowPos;

    public Vector2 HalfUV => 0.5f * Vector2.One / zoom;
    public Vector2 UVMin => Vector2.Clamp(pan - HalfUV, Vector2.Zero, Vector2.One);
    public Vector2 UVMax => Vector2.Clamp(pan + HalfUV, Vector2.Zero, Vector2.One);

    IReadOnlyDictionary<Waymark, Vector3> markers;

    public MapView(Map map)
    {
        this.map = map;
        sizePx = new(200, 200);
        pan = new(0.5f);
        zoom = 1;
        markers = new Dictionary<Waymark, Vector3>();
    }

    public void FocusMarkers(IReadOnlyDictionary<Waymark, Vector3> markers)
    {
        this.markers = markers;
        var bb = AABB.BoundingPoints(markers.Values);

        ZoomToFit(bb);

        var center = bb.Center.XZ();
        if (Vector2.Distance(center, map.Center) < 20)
            center = map.Center;
        Pan(center);
    }

    public void Pan(Vector2 wPos)
    {
        pan = map.WorldToNormTexCoords(wPos);
        pan = Vector2.Clamp(pan, HalfUV, Vector2.One - HalfUV);
    }

    public void ZoomToFit(AABB bb)
    {
        var boundingBoxSize = MathF.Max(bb.LongAxisLength, 30);
        zoom = 0.6f / (boundingBoxSize * map.WorldToNormTexScale);
        zoom = Math.Clamp(zoom, 1f, 1000f);
    }

    private Vector2 W2S(Vector3 worldCoords)
    {
        var texCoords = map.WorldToNormTexCoords(worldCoords);
        var screenCoords = texCoords;
        screenCoords = (screenCoords - UVMin) / (UVMax - UVMin) * sizePx;
        screenCoords += windowPos;
        return screenCoords;
    }

    public void Draw()
    {
        using (var child = ImRaii.Child(map.Id, sizePx))
        {
            if (child.Success)
            {
                var texture = map.Texture;
                if (texture == null) return;

                windowPos = ImGui.GetItemRectMin();
                ImGui.Image(texture.Handle, sizePx, UVMin, UVMax);
                ImGui.GetWindowDrawList().AddText(windowPos + new Vector2(6, 1) * ImGuiHelpers.GlobalScale, 0xFF000000, map.Name);
                ImGui.GetWindowDrawList().AddText(windowPos + new Vector2(5, 0) * ImGuiHelpers.GlobalScale, 0xFFFFFFFF, map.Name);

                foreach ((Waymark w, Vector3 wPos) in markers)
                {
                    if (Waymarks.IsSquare(w))
                        AddSquare(wPos, Waymarks.SquareHalfWidth, Waymarks.GetColor(w));
                    if (Waymarks.IsCircle(w))
                        AddCircle(wPos, Waymarks.CircleRadius, Waymarks.GetColor(w));
                }
                foreach ((Waymark w, Vector3 wPos) in markers)
                {
                    var wrap = Plugin.TextureProvider.GetFromGameIcon(Waymarks.GetIconId(w)).GetWrapOrEmpty();
                    if (wrap != null)
                    {
                        ImGui.GetWindowDrawList().AddImage(
                            wrap.Handle,
                            W2S(wPos) - WaymarkMapIconHalfSizePx,
                            W2S(wPos) + WaymarkMapIconHalfSizePx,
                            Vector2.Zero,
                            Vector2.One,
                            0xFF000000);
                    }
                }
                foreach ((Waymark w, Vector3 wPos) in markers)
                {
                    var wrap = Plugin.TextureProvider.GetFromGameIcon(Waymarks.GetIconId(w)).GetWrapOrEmpty();
                    if (wrap != null)
                    {
                        ImGui.GetWindowDrawList().AddImage(
                            wrap.Handle,
                            W2S(wPos) - WaymarkMapIconHalfSizePx,
                            W2S(wPos) + WaymarkMapIconHalfSizePx,
                            Vector2.Zero,
                            Vector2.One);
                    }
                }
            }
        }
    }
}
