using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using ImGuiNET;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace WaymarkStudio;
internal static class Extensions
{
    public static IDictionary<TKey, TValue> Clone<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
    {
        return dictionary.ToDictionary(entry => entry.Key,
                                       entry => entry.Value);
    }

    internal static Vector2 XZ(this Vector3 v)
    {
        return new Vector2(v.X, v.Z);
    }

    // The game rounds world positions floats for waymarks to the thousandth decimal.
    internal static float Round(this float value)
    {
        return (int)(value * 1000) / 1000f;
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

    public static MarkerPresetPlacement ToMarkerPresetPlacement(this FieldMarkerPreset preset)
    {
        MarkerPresetPlacement placementStruct = new();
        for (int i = 0; i < 8; i++)
        {
            placementStruct.Active[i] = preset.IsMarkerActive(i);
            placementStruct.X[i] = preset.Markers[i].X;
            placementStruct.Y[i] = preset.Markers[i].Y;
            placementStruct.Z[i] = preset.Markers[i].Z;
        }
        return placementStruct;
    }

    internal static ushort TerritoryIdForContendId(ushort contentId)
    {
        var contentSheet = Plugin.DataManager.GetExcelSheet<ContentFinderCondition>();
        if (contentSheet.TryGetRow(contentId, out var row))
            return (ushort)row.TerritoryType.Value.RowId;
        return 0;
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
        WaymarkPreset p = new(name, TerritoryIdForContendId(preset.ContentFinderConditionId), preset.ContentFinderConditionId, null, DateTimeOffset.FromUnixTimeSeconds(preset.Timestamp));
        foreach (Waymark w in Enum.GetValues<Waymark>())
        {
            int index = (int)w;
            bool active = preset.IsMarkerActive(index);
            if (active)
                p.MarkerPositions.Add(w, preset.Markers[index].ToWorldPosition());
        }
        return p;
    }

    public static IReadOnlyDictionary<Waymark, Vector3> ActiveMarkers(this MarkingController controller)
    {
        var activeFieldMarkers = new Dictionary<Waymark, Vector3>();
        for (int i = 0; i < controller.FieldMarkers.Length; i++)
        {
            var marker = controller.FieldMarkers[i];
            if (marker.Active)
                activeFieldMarkers.Add((Waymark)i, marker.Position);
        }
        return activeFieldMarkers;
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
}
