using Dalamud.Game.ClientState.Objects.SubKinds;
using Newtonsoft.Json;
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

    [JsonConstructor]
    public CircleTrigger(string Name, uint TerritoryId)
    {
        this.Name = Name;
        this.TerritoryId = TerritoryId;
        Radius = 5;
    }

    public CircleTrigger(CircleTrigger other) : this(other.Name, other.TerritoryId)
    {
        CopyFrom(other);
    }

    public void CopyFrom(CircleTrigger other)
    {
        Name = other.Name;
        TerritoryId = other.TerritoryId;
        Center = other.Center;
        Radius = other.Radius;
    }

    public void Draw()
    {
        if (Plugin.Overlay.CanDraw)
            Plugin.Overlay.list.Add((PctDrawList drawList) =>
            {
                uint color = Contains(Plugin.ClientState.LocalPlayer.Position) ? 0xFFFFFFFF : 0xFF000000;
                drawList.AddCircle(Center, Radius, color);

                int numSegments = Math.Max(2, (int)(Radius / 2.5)) * 8;

                float angleStep = MathF.PI * 2 / numSegments;

                var radius1 = MathF.Max(0, Radius - 1);
                var radius2 = MathF.Max(0, Radius - 2);
                var radius3 = MathF.Max(0, Radius - 3);

                var white0 = 0xFFFFFFFF;
                var black0 = 0xFF000000;
                for (int step = 0; step < numSegments; step++)
                {
                    uint color0 = step % 2 == 0 ? white0 : black0;
                    uint color1 = step % 2 == 0 ? black0 : white0;

                    drawList.AddFanFilled(Center, radius1, Radius, step * angleStep, (step + 1) * angleStep, color0.WithAlpha(125), color0.WithAlpha(150));
                    drawList.AddFanFilled(Center, radius2, radius1, step * angleStep, (step + 1) * angleStep, color1.WithAlpha(100), color1.WithAlpha(125));
                    drawList.AddFanFilled(Center, radius3, radius2, step * angleStep, (step + 1) * angleStep, color0.WithAlpha(75), color0.WithAlpha(100));
                }
            });
    }

    public bool Contains(IPlayerCharacter? character)
    {
        if (character == null) return false;
        return Contains(character.Position);
    }

    public bool Contains(Vector3 point)
    {
        return Vector2.DistanceSquared(point.XZ(), Center.XZ()) < Radius * Radius;
    }
}
