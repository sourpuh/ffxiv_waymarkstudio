using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using FieldMarker = FFXIVClientStructs.FFXIV.Client.Game.UI.FieldMarker;

namespace WaymarkStudio;
internal static class Extensions
{
    public static IDictionary<TKey, TValue> Clone<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
    {
        return dictionary.ToDictionary(entry => entry.Key,
                                       entry => entry.Value);
    }

    public static bool DeepEquals<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IDictionary<TKey, TValue> otherDictionary)
    {
        return dictionary.Equals(otherDictionary)
            || dictionary.OrderBy(kv => kv.Key).SequenceEqual(otherDictionary.OrderBy(kv => kv.Key));
    }

    public static bool DeepEquals<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, IReadOnlyDictionary<TKey, TValue> otherDictionary)
    {
        return dictionary.Equals(otherDictionary)
            || dictionary.OrderBy(kv => kv.Key).SequenceEqual(otherDictionary.OrderBy(kv => kv.Key));
    }

    public static void Move<T>(this IList<T> list, int sourceIndex, int targetIndex)
    {
        var item = list[sourceIndex];
        if (sourceIndex < targetIndex)
        {
            list.Insert(targetIndex + 1, item);
            list.RemoveAt(sourceIndex);
        }
        else if (targetIndex < sourceIndex)
        {
            list.RemoveAt(sourceIndex);
            list.Insert(targetIndex, item);
        }
    }

    extension(Vector3 v)
    {
        public Vector2 XZ => new Vector2(v.X, v.Z);
    }

    // The game rounds world positions floats for waymarks to the thousandth decimal.
    internal static float Round(this float value)
    {
        return MathF.Round(value, 3);
    }

    internal static Vector3 Round(this Vector3 v)
    {
        v.X = v.X.Round();
        v.Y = v.Y.Round();
        v.Z = v.Z.Round();
        return v;
    }

    internal static Vector3 ToWorldPosition(this GamePresetPoint point)
    {
        Vector3 wPos;
        wPos.X = point.X / 1000f;
        wPos.Y = point.Y / 1000f;
        wPos.Z = point.Z / 1000f;
        return wPos;
    }

    internal static GamePresetPoint ToGamePresetPoint(this Vector3 wPos)
    {
        GamePresetPoint point;
        point.X = (int)(wPos.X * 1000);
        point.Y = (int)(wPos.Y * 1000);
        point.Z = (int)(wPos.Z * 1000);
        return point;
    }

    public static MarkerPresetPlacement ToMarkerPresetPlacement(this WaymarkPreset preset)
    {
        MarkerPresetPlacement placementStruct = new();
        foreach (Waymark w in Enum.GetValues<Waymark>())
        {
            var i = (int)w;
            // TODO necessary?
            placementStruct.Active[i] = false;
            if (preset.MarkerPositions.TryGetValue(w, out var position))
            {
                var point = position.ToGamePresetPoint();
                placementStruct.Active[i] = true;
                placementStruct.X[i] = point.X;
                placementStruct.Y[i] = point.Y;
                placementStruct.Z[i] = point.Z;
            }
        }
        return placementStruct;
    }

    public static string GetName(this TerritoryType territory)
    {
        var mapName = territory.PlaceName.Value.Name.ExtractText();
        if (territory.ContentFinderCondition.RowId != 0)
        {
            mapName = territory.ContentFinderCondition.Value.Name.ExtractText();
        }
        return mapName;
    }

    public static uint GetContentId(this TerritoryType territory)
    {
        var mapName = territory.PlaceName.Value.Name.ExtractText();
        if (territory.ContentFinderCondition.IsValid)
        {
            return territory.ContentFinderCondition.RowId;
        }
        return 0;
    }

    public static WaymarkPreset ToPreset(this FieldMarkerPreset preset, string name = "")
    {
        if (preset.ContentFinderConditionId == 0) return new(name);
        WaymarkPreset p = new(name, TerritorySheet.TerritoryIdForContentId(preset.ContentFinderConditionId), null, DateTimeOffset.FromUnixTimeSeconds(preset.Timestamp));
        foreach (Waymark w in Enum.GetValues<Waymark>())
        {
            int index = (int)w;
            bool active = preset.IsMarkerActive(index);
            if (active)
                p.MarkerPositions.Add(w, preset.Markers[index].ToWorldPosition());
        }
        return p;
    }

    public static IReadOnlyDictionary<Waymark, Vector3> ToDict(this ReadOnlySpan<FieldMarker> markers)
    {
        var activeFieldMarkers = new Dictionary<Waymark, Vector3>();
        for (int i = 0; i < markers.Length; i++)
        {
            var marker = markers[i];
            if (marker.Active)
                activeFieldMarkers.Add((Waymark)i, marker.Position);
        }
        return activeFieldMarkers;
    }

    public static int CountDiffs(this IReadOnlyDictionary<Waymark, Vector3> first, IReadOnlyDictionary<Waymark, Vector3> second)
    {
        int diffCount = 0;
        foreach (var kvp in first)
        {
            if (!second.TryGetValue(kvp.Key, out var secondValue))
            {
                diffCount++;
            }
            else if (kvp.Value != secondValue)
            {
                diffCount++;
            }
        }

        foreach (var key in second.Keys)
        {
            if (!first.ContainsKey(key))
            {
                diffCount++;
            }
        }
        return diffCount;
    }

    public static long GetUserVisibleHashCode(this IDictionary<Waymark, Vector3> dictionary)
    {
        int hash = 0;
        foreach (var kvp in dictionary)
        {
            int entryHash = HashCode.Combine(kvp.Key, kvp.Value);
            hash ^= entryHash;
        }
        return (long)int.MaxValue + hash;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ToUint(this Vector4 color)
    {
        return ImGui.ColorConvertFloat4ToU32(color);
    }

    public static uint WithAlpha(this uint color, byte alpha)
    {
        return (uint)(color & 0x00FFFFFF | alpha << 24);
    }

    /**
     * Compute a normal for RaycastHit. RaycastHit's standard normal is inaccurate.
     */
    internal static Vector3 ComputeNormal(this RaycastHit hit)
    {
        // Compute normal by taking cross product of the edges of the vertex hit.
        Vector3 edge1 = hit.V2 - hit.V1;
        Vector3 edge2 = hit.V3 - hit.V1;
        return Vector3.Normalize(Vector3.Cross(edge1, edge2));
    }

    public static void Write7BitEncodedIntSigned(this BinaryWriter writer, int value)
    {
        writer.Write7BitEncodedInt((value << 1) ^ (value >> 31));
    }

    public static int Read7BitEncodedIntSigned(this BinaryReader reader)
    {
        int value = reader.Read7BitEncodedInt();
        return (value >>> 1) ^ -(value & 1);
    }

    public static void Write(this BinaryWriter writer, Vector3 value)
    {
        writer.Write7BitEncodedIntSigned((int)MathF.Round(value.X * 1000));
        writer.Write7BitEncodedIntSigned((int)MathF.Round(value.Y * 1000));
        writer.Write7BitEncodedIntSigned((int)MathF.Round(value.Z * 1000));
    }

    public static Vector3 ReadVector3(this BinaryReader reader)
    {
        var x = reader.Read7BitEncodedIntSigned() / 1000f;
        var y = reader.Read7BitEncodedIntSigned() / 1000f;
        var z = reader.Read7BitEncodedIntSigned() / 1000f;
        return new(x, y, z);
    }

    public static void Write(this BinaryWriter writer, Vector2 value)
    {
        writer.Write7BitEncodedIntSigned((int)MathF.Round(value.X * 1000));
        writer.Write7BitEncodedIntSigned((int)MathF.Round(value.Y * 1000));
    }

    public static Vector3 ReadXZVector2(this BinaryReader reader)
    {
        var x = reader.Read7BitEncodedIntSigned() / 1000f;
        var z = reader.Read7BitEncodedIntSigned() / 1000f;
        return new Vector3(x, 0, z);
    }

    public static string RecursiveInnerMessage(this Exception e)
    {
        if (e.InnerException != null)
            return RecursiveInnerMessage(e.InnerException);
        return e.Message;
    }
}
