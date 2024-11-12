using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;


namespace WaymarkStudio;

using ContentType = FFXIVClientStructs.FFXIV.Client.Game.Event.ContentType;

/**
 * Manager for drafting and interacting with native waymarks.
 */
internal class WaymarkManager
{
    private static readonly IReadOnlyDictionary<Waymark, Vector3> EmptyWaymarks = new Dictionary<Waymark, Vector3>();
    private static readonly TimeSpan MinPlacementFrequency = TimeSpan.FromSeconds(0.25);

    internal bool showGuide = false;
    internal CircleGuide circleGuide;
    internal ushort territoryId;
    internal ushort contentFinderId;
    internal string mapName;
    internal Dictionary<Waymark, Vector3> placeholders = new();
    internal IReadOnlyDictionary<Waymark, Vector3> hoverPreviews = EmptyWaymarks;
    private readonly Stopwatch lastPlacementTimer;

    private unsafe delegate byte PlaceWaymark(MarkingController* markingController, uint marker, Vector3 wPos);
    private readonly PlaceWaymark? placeWaymarkFn;

    private unsafe delegate byte ClearWaymarks(MarkingController* markingController);
    private readonly ClearWaymarks? clearWaymarksFn;

    private unsafe delegate byte WaymarkSafety();
    private readonly WaymarkSafety? waymarkSafetyFn;

    internal WaymarkPreset Preset { get { return new(mapName, territoryId, contentFinderId, new Dictionary<Waymark, Vector3>(placeholders)); } }
    internal IReadOnlyDictionary<Waymark, Vector3> Placeholders => placeholders;
    internal IReadOnlyDictionary<Waymark, Vector3> HoverPreviews => hoverPreviews;
    internal unsafe IReadOnlyDictionary<Waymark, Vector3> Waymarks => MarkingController.Instance()->ActiveMarkers();

    public WaymarkManager()
    {
        lastPlacementTimer = new();
        lastPlacementTimer.Start();
        placeWaymarkFn = Marshal.GetDelegateForFunctionPointer<PlaceWaymark>(Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 84 C0 0F 85 ?? ?? ?? ?? EB 23"));
        clearWaymarksFn = Marshal.GetDelegateForFunctionPointer<ClearWaymarks>(Plugin.SigScanner.ScanText("41 55 48 83 EC 50 4C 8B E9"));
        waymarkSafetyFn = Marshal.GetDelegateForFunctionPointer<WaymarkSafety>(Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 84 C0 74 0D B0 05"));
    }

    internal void OnTerritoryChange(TerritoryType territory)
    {
        territoryId = (ushort)territory.RowId;
        mapName = territory.PlaceName.Value.Name.ExtractText();
        contentFinderId = (ushort)territory.ContentFinderCondition.RowId;
        if (contentFinderId != 0)
        {
            mapName = territory.ContentFinderCondition.Value.Name.ExtractText();
        }
        placeholders.Clear();
        hoverPreviews = EmptyWaymarks;
        showGuide = false;
        circleGuide = new(radius: 1);
    }
    internal void ClearPlaceholders()
    {
        placeholders.Clear();
    }
    internal unsafe byte NativeClearWaymarks()
    {
        return clearWaymarksFn?.Invoke(MarkingController.Instance()) ?? 0;
    }

    public void SetHoverPreview(WaymarkPreset preset)
    {
        if (preset.TerritoryId != territoryId) return;
        hoverPreviews = (IReadOnlyDictionary<Waymark, Vector3>)preset.MarkerPositions;
    }

    public void ClearHoverPreview()
    {
        hoverPreviews = EmptyWaymarks;
    }

    private bool IsPointGrounded(Vector3 point)
    {
        point = point.Round();
        const float castHeight = 1f;
        Vector3 castOffset = new(0, castHeight / 2, 0);
        Vector3 castOrigin = point + castOffset;
        if (BGCollisionModule.RaycastMaterialFilter(castOrigin, -Vector3.UnitY, out RaycastHit hitInfo, castHeight))
        {
            return point == hitInfo.Point.Round();
        }
        return false;
    }

    internal PlacementUnsafeReason GeneralWaymarkPlacementStatus()
    {
        if (Plugin.ClientState.LocalPlayer == null)
            return PlacementUnsafeReason.NoLocalPlayer;
        if (!IsSupportedZone())
            return PlacementUnsafeReason.UnsupportedZone;
        if (Plugin.Condition[ConditionFlag.InCombat])
            return PlacementUnsafeReason.InCombat;
        if (Plugin.Condition[ConditionFlag.DutyRecorderPlayback])
            return PlacementUnsafeReason.DutyRecorderPlayback;
        return PlacementUnsafeReason.Safe;
    }

    internal bool IsSafeToPlaceWaymarks()
    {
        return GeneralWaymarkPlacementStatus() == PlacementUnsafeReason.Safe;
    }

    internal PlacementUnsafeReason WaymarkPlacementStatus(Vector3 wPos)
    {
        var status = GeneralWaymarkPlacementStatus();
        if (status != PlacementUnsafeReason.Safe)
            return status;
        if (!IsPointGrounded(wPos))
            return PlacementUnsafeReason.NotGrounded;
        if (Vector3.Distance(wPos, Plugin.ClientState.LocalPlayer.Position) > 200)
            return PlacementUnsafeReason.TooFar;
        if (lastPlacementTimer.Elapsed < MinPlacementFrequency)
            return PlacementUnsafeReason.TooFrequent;
        return PlacementUnsafeReason.Safe;
    }

    public void SetPlaceholderPreset(WaymarkPreset preset)
    {
        if (preset.TerritoryId != territoryId) return;
        placeholders = new(preset.MarkerPositions);
    }

    internal void PlaceWaymarkPlaceholder(Waymark waymark, Vector3 wPos, float castHeight = 10)
    {
        Vector3 castOffset = new(0, castHeight / 2, 0);
        Vector3 castOrigin = wPos + castOffset;
        if (BGCollisionModule.RaycastMaterialFilter(castOrigin, -Vector3.UnitY, out RaycastHit hit, castHeight))
        {
            var d = Vector3.Dot(hit.ComputeNormal(), Vector3.UnitY);
            if (d >= 0.7f)
            {
                placeholders[waymark] = hit.Point.Round();
                return;
            }
        }
        ClearWaymarkPlaceholder(waymark);
    }
    internal void ClearWaymarkPlaceholder(Waymark waymark)
    {
        placeholders.Remove(waymark);
    }

    internal async void PlaceWaymarkWithRetries(Waymark waymark, Vector3 wPos, bool clearPlaceholder = true)
    {
        ushort territoryId = this.territoryId;
        await Plugin.Framework.Run(async () =>
        {
            // TODO tidy
            while (SafePlaceWaymark(waymark, wPos))
            {
                await Plugin.Framework.DelayTicks(100);
                if (territoryId != this.territoryId)
                {
                    return;
                }
            }
            if (clearPlaceholder && placeholders.GetValueOrDefault(waymark) == wPos)
                placeholders.Remove(waymark);
        });
    }

    internal bool SafePlaceWaymark(Waymark waymark, Vector3 wPos)
    {
        var reason = WaymarkPlacementStatus(wPos);
        if (reason == PlacementUnsafeReason.Safe)
        {
            lastPlacementTimer.Restart();
            return UnsafeNativePlaceWaymark(waymark, wPos);
        }
        return false;
    }

    internal unsafe bool IsSupportedZone()
    {
        return (waymarkSafetyFn?.Invoke() ?? 0) == 0;
    }

    private unsafe bool UnsafeNativePlaceWaymark(Waymark waymark, Vector3 wPos)
    {
        var status = placeWaymarkFn?.Invoke(MarkingController.Instance(), (uint)waymark, wPos);
        if (status != 0)
        {
            Plugin.Chat.Print("[Report to dev] Native placement failed with status " + status);
        }
        return status == 0;
    }

    internal bool IsSafeToDirectPlacePreset()
    {
        return DirectPlacementStatus() is PlacementUnsafeReason.Safe or PlacementUnsafeReason.NoWaymarksPlaced;
    }

    internal PlacementUnsafeReason DirectPlacementStatus()
    {
        if (Plugin.ClientState.LocalPlayer == null)
            return PlacementUnsafeReason.NoLocalPlayer;
        if (Plugin.Condition[ConditionFlag.InCombat])
            return PlacementUnsafeReason.InCombat;
        if (EventFramework.GetCurrentContentType() is not ContentType.Instance)
            return PlacementUnsafeReason.NotInInstance;
        if (Placeholders.Count == 0)
            return PlacementUnsafeReason.NoWaymarksPlaced;
        return PlacementUnsafeReason.Safe;
    }

    public void SafePlacePreset(WaymarkPreset preset, bool clearPlaceholder = true)
    {
        if (preset.MarkerPositions.Count > 0)
            if (IsSafeToDirectPlacePreset())
            {
                UnsafeNativePlacePreset(preset.ToGamePreset());
                if (clearPlaceholder) Plugin.WaymarkManager.ClearPlaceholders();
            }
            else
            {
                foreach (Waymark w in Enum.GetValues<Waymark>())
                {
                    if (preset.MarkerPositions.TryGetValue(w, out var wPos))
                        Plugin.WaymarkManager.PlaceWaymarkWithRetries(w, wPos, clearPlaceholder);
                }
            }
    }

    private unsafe void UnsafeNativePlacePreset(FieldMarkerPreset preset)
    {
        var placementStruct = preset.ToMarkerPresetPlacement();
        MarkingController.Instance()->PlacePreset(&placementStruct);
    }

    internal enum PlacementUnsafeReason
    {
        Safe,
        NoLocalPlayer,
        InCombat,
        DutyRecorderPlayback,
        NotInInstance,
        NoWaymarksPlaced,
        TooFar,
        NotGrounded,
        TooFrequent,
        UnsupportedZone,
    }
}
