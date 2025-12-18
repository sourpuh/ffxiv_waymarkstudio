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
    [JsonIgnore] private readonly string transientId = Guid.NewGuid().ToString();
    [JsonIgnore] public bool Editing;

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
        var alpha = Contains(Plugin.ObjectTable.LocalPlayer!.Position) ? 0.8f : 0.4f;
        if (Editing)
        {
            var pulse = (Environment.TickCount64 % 2000) / 1000f;
            if (pulse > 1) pulse = 2f - pulse;
            alpha = 0.2f + alpha * pulse;
        }
        var color = Vector4.One;
        color.W = alpha;
        PictoService.VfxRenderer.AddOmen(transientId, "general01bf", Center, new(Radius, 100, Radius), 0, color);
    }

    public bool Contains(IPlayerCharacter? character)
    {
        if (character == null) return false;
        return Contains(character.Position);
    }

    public bool Contains(Vector3 point)
    {
        return Vector2.DistanceSquared(point.XZ, Center.XZ) < Radius * Radius;
    }
}
