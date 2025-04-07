using System.Numerics;

namespace WaymarkStudio.Compat.WaymarkPresetPlugin;
public class WPPWaymark
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public int ID { get; set; }
    public bool Active { get; set; }

    public Vector3 Position => new(X, Y, Z);
}
