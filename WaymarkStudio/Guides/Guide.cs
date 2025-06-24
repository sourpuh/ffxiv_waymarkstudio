using Pictomancy;
using System.Collections.Generic;
using System.Numerics;

namespace WaymarkStudio.Guides;
public abstract class Guide
{
    public Vector3 center;

    public abstract void Draw(PctDrawList drawList);
    public abstract Vector3 North { get; }
    public abstract Vector3 NorthEast { get; }
    public abstract Vector3 East { get; }
    public abstract Vector3 SouthEast { get; }
    public abstract Vector3 South { get; }
    public abstract Vector3 SouthWest { get; }
    public abstract Vector3 West { get; }
    public abstract Vector3 NorthWest { get; }
    public abstract IEnumerable<Vector3> SnapPoints { get; }
}
