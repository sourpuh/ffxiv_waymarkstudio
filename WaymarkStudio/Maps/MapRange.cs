using System.Numerics;
using static Lumina.Data.Parsing.Layer.LayerCommon;

namespace WaymarkStudio.Maps;

// A bounding box for one of the game's maps.
// When a point is inside the box, it should be displayed on that map.
// Technically the game can use non box shaped map ranges, but in practice a box is good enough.
internal class MapRange : AABB
{
    internal readonly uint MapId;
    public MapRange(InstanceObject o) : base(o.Transform)
    {
        var mapRange = (MapRangeInstanceObject)o.Object;
        MapId = mapRange.Map;
    }

    public MapRange(Vector3 min, Vector3 max, uint mapId) : base(min, max)
    {
        MapId = mapId;
    }
}
