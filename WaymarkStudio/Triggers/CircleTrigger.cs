using Pictomancy;
using System;
using System.Numerics;

namespace WaymarkStudio.Triggers;
public class CircleTrigger
{
    public string Name;
    public uint TerritoryId;
    public Vector3 Center;
    public float Radius;

    public CircleTrigger(string Name, uint TerritoryId)
    {
        this.Name = Name;
        this.TerritoryId = TerritoryId;
        Radius = 5;
    }

    public void Draw()
    {
        Plugin.Overlay.list.Add((PctDrawList drawList) =>
        {
            drawList.AddCircle(Center, Radius, 0xFF00FFFF);
            if (Contains(Plugin.ClientState.LocalPlayer.Position))
            {
                drawList.AddFanFilled(Center, MathF.Max(0, Radius - 2), Radius, 0, MathF.PI * 2, 0x0000FFFF, 0x7000FFFF);
            }
        });
    }

    public bool Contains(Vector3 point)
    {
        return Vector2.DistanceSquared(point.XZ(), Center.XZ()) < Radius * Radius;
    }
}
