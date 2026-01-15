using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using WaymarkStudio.Tasks;
using TerritoryInfo = WaymarkStudio.TerritorySheet.TerritoryInfo;
using FieldMarker = FFXIVClientStructs.FFXIV.Client.Game.UI.FieldMarker;
using System.Threading.Tasks;

namespace WaymarkStudio;
/**
 * Manager for drafting and interacting with native waymarks.
 */
internal class WaymarkManager
{
    private static readonly IReadOnlyDictionary<Waymark, Vector3> EmptyWaymarks = new Dictionary<Waymark, Vector3>();

    internal ushort territoryId;
    internal TerritoryInfo territoryInfo;
    internal Dictionary<Waymark, Vector3> draftMarkers = new();
    internal IReadOnlyDictionary<Waymark, Vector3> hoverPreviews = EmptyWaymarks;
    internal CancellationTokenSource? taskCancelToken;

    /// <summary>
    /// A merging of draft markers and placed waymarks
    /// </summary>
    internal WaymarkPreset MergedDraftPreset
    {
        get
        {
            foreach ((Waymark w, Vector3 p) in Waymarks)
                if (!draftMarkers.ContainsKey(w))
                    draftMarkers.Add(w, p);
            return new(territoryInfo.Name, territoryId, new Dictionary<Waymark, Vector3>(draftMarkers));
        }
    }
    internal WaymarkPreset DraftPreset { get { return new(territoryInfo.Name, territoryId, new Dictionary<Waymark, Vector3>(draftMarkers)); } }
    internal WaymarkPreset WaymarkPreset { get { return new(territoryInfo.Name, territoryId, new Dictionary<Waymark, Vector3>(Waymarks)); } }

    internal IReadOnlyDictionary<Waymark, Vector3> DraftMarkers => draftMarkers;
    internal IReadOnlyDictionary<Waymark, Vector3> HoverPreviews => hoverPreviews;
    internal IReadOnlyDictionary<Waymark, Vector3> Waymarks = EmptyWaymarks;

    internal unsafe void OnTerritoryChange(TerritoryType territory)
    {
        territoryInfo = TerritorySheet.GetInfo((ushort)territory.RowId);
        territoryId = territoryInfo.Id;
        draftMarkers.Clear();
        hoverPreviews = EmptyWaymarks;
        taskCancelToken?.Cancel();
        var currentFieldMarkers = MarkingController.Instance()->FieldMarkers;
        prevFieldMarkers = currentFieldMarkers.ToArray();
        Waymarks = currentFieldMarkers.ToDict();
    }

    private Task? placePresetTask;
    private ReadOnlyMemory<FieldMarker> prevFieldMarkers;
    public delegate void OnPresetPlacedDelegate(IReadOnlyDictionary<Waymark, Vector3> newWaymarks, bool placedByMe);
    public event OnPresetPlacedDelegate? OnPresetPlaced;
    public unsafe void Update()
    {
        var currentFieldMarkers = MarkingController.Instance()->FieldMarkers;
        if (!currentFieldMarkers.SequenceEqual(prevFieldMarkers.Span))
        {
            var prevWaymarks = Waymarks;
            Waymarks = currentFieldMarkers.ToDict();
            var diffCount = Waymarks.CountDiffs(prevWaymarks);
            if (diffCount >= 2 && Waymarks.Count > 2)
            {
                var placedByMe = placePresetTask != null;
                OnPresetPlaced?.Invoke(Waymarks, placedByMe);
            }
            prevFieldMarkers = currentFieldMarkers.ToArray();
        }

        if (placePresetTask?.IsCompleted ?? false)
        {
            placePresetTask = null;
        }
    }

    internal bool WaymarksUnsupported => !territoryInfo.AreWaymarksSupported;
    internal bool HasWaymarks => Waymarks.Count > 0;
    internal bool HasDraftMarkers => draftMarkers.Count > 0;
    internal void ClearDraftMarkers()
    {
        draftMarkers.Clear();
    }

    internal unsafe void ClearWaymark(Waymark waymark)
    {
        if (Plugin.WaymarkManager.IsSafeToPlaceWaymarks())
        {
            taskCancelToken?.Cancel();
            taskCancelToken = new();
            ClearWaymarkTask.Start(taskCancelToken, waymark);
        }
    }

    internal unsafe void ClearWaymarks()
    {
        taskCancelToken?.Cancel();
        taskCancelToken = new();
        ClearWaymarksTask.Start(taskCancelToken);
    }

    public void PlaceDraftOrWaymark(Waymark waymark, Vector3 wPos)
    {
        draftMarkers[waymark] = wPos;

        if (Plugin.Config.PlaceRealIfPossible)
        {
            taskCancelToken?.Cancel();
            taskCancelToken = new();
            PlaceWaymarkTask.Start(taskCancelToken, waymark, wPos);
        }
    }

    public void PlacePreset(WaymarkPreset preset)
    {
        taskCancelToken?.Cancel();
        taskCancelToken = new();
        SetDraftPreset(preset);
        ClearHoverPreview();
        placePresetTask = PlaceWaymarkPresetTask.Start(taskCancelToken, preset);
    }

    public bool IsPlayerWithinTraceDistance(WaymarkPreset preset)
    {
        if (Plugin.ObjectTable.LocalPlayer == null)
            return false;
        return preset.DistanceToNearestNonAdjustedMarker(Plugin.ObjectTable.LocalPlayer.Position) < 200;
    }

    public void SetHoverPreview(WaymarkPreset preset)
    {
        if (!preset.IsCompatibleTerritory(territoryId)) return;
        hoverPreviews = (IReadOnlyDictionary<Waymark, Vector3>)preset.MarkerPositions;
    }

    public void ClearHoverPreview()
    {
        hoverPreviews = EmptyWaymarks;
    }

    private bool IsPointGrounded(Vector3 point, float epsilon = 0.001f)
    {
        const float castHeight = 1f;
        Vector3 castOffset = new(0, castHeight / 2, 0);
        Vector3 castOrigin = point + castOffset;
        if (Raycaster.Raycast(castOrigin, -Vector3.UnitY, out RaycastHit hitInfo, castHeight))
        {
            var delta = MathF.Abs(point.Y - hitInfo.Point.Y).Round();
            return delta <= epsilon;
        }
        return false;
    }

    internal PlacementUnsafeReason GeneralWaymarkPlacementStatus()
    {
        if (Plugin.ObjectTable.LocalPlayer == null)
            return PlacementUnsafeReason.NoLocalPlayer;
        if (WaymarksUnsupported)
            return PlacementUnsafeReason.UnsupportedArea;
        if (Plugin.Condition[ConditionFlag.InCombat])
            return PlacementUnsafeReason.InCombat;
        if (IsOccupied())
            return PlacementUnsafeReason.Occupied;
        if (Plugin.Condition[ConditionFlag.DutyRecorderPlayback])
            return PlacementUnsafeReason.DutyRecorderPlayback;
        return PlacementUnsafeReason.Safe;
    }

    public static bool IsOccupied()
    {
        var Condition = Plugin.Condition;
        return Condition[ConditionFlag.Occupied]
               || Condition[ConditionFlag.Occupied30]
               || Condition[ConditionFlag.Occupied33]
               || Condition[ConditionFlag.Occupied38]
               || Condition[ConditionFlag.Occupied39]
               || Condition[ConditionFlag.OccupiedInCutSceneEvent]
               || Condition[ConditionFlag.OccupiedInEvent]
               || Condition[ConditionFlag.OccupiedInQuestEvent]
               || Condition[ConditionFlag.OccupiedSummoningBell]
               || Condition[ConditionFlag.WatchingCutscene]
               || Condition[ConditionFlag.WatchingCutscene78]
               || Condition[ConditionFlag.BetweenAreas]
               || Condition[ConditionFlag.BetweenAreas51]
               || Condition[ConditionFlag.InThatPosition]
               || Condition[ConditionFlag.Crafting]
               || Condition[ConditionFlag.ExecutingCraftingAction]
               || Condition[ConditionFlag.PreparingToCraft]
               || Condition[ConditionFlag.InThatPosition]
               || Condition[ConditionFlag.Unconscious]
               || Condition[ConditionFlag.MeldingMateria]
               || Condition[ConditionFlag.Gathering]
               || Condition[ConditionFlag.ExecutingGatheringAction]
               || Condition[ConditionFlag.OperatingSiegeMachine]
               || Condition[ConditionFlag.CarryingItem]
               || Condition[ConditionFlag.CarryingObject]
               || Condition[ConditionFlag.BeingMoved]
               || Condition[ConditionFlag.RidingPillion]
               || Condition[ConditionFlag.Mounting]
               || Condition[ConditionFlag.Mounting71]
               || Condition[ConditionFlag.ParticipatingInCustomMatch]
               || Condition[ConditionFlag.PlayingLordOfVerminion]
               || Condition[ConditionFlag.ChocoboRacing]
               || Condition[ConditionFlag.PlayingMiniGame]
               || Condition[ConditionFlag.Performing]
               || Condition[ConditionFlag.PreparingToCraft]
               || Condition[ConditionFlag.Fishing]
               || Condition[ConditionFlag.Transformed]
               || Condition[ConditionFlag.UsingHousingFunctions]
               || Plugin.ObjectTable.LocalPlayer?.IsTargetable != true;
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
        if (Plugin.Config.DisableWorldPresetSafetyChecks)
            return PlacementUnsafeReason.Safe;
        if (Vector3.Distance(wPos, Plugin.ObjectTable.LocalPlayer!.Position) > 200)
            return PlacementUnsafeReason.TooFar;
        if (!IsPointGrounded(wPos))
            return PlacementUnsafeReason.NotGrounded;
        return PlacementUnsafeReason.Safe;
    }

    public void SetDraftPreset(WaymarkPreset preset)
    {
        if (!preset.IsCompatibleTerritory(territoryId)) return;
        draftMarkers = new(preset.MarkerPositions);
    }

    internal void TraceAndPlaceDraftMarker(Waymark waymark, Vector3 wPos, float castHeight = 10)
    {
        Vector3 castOffset = new(0, castHeight / 2, 0);
        Vector3 castOrigin = wPos + castOffset;
        if (Raycaster.Raycast(castOrigin, -Vector3.UnitY, out RaycastHit hit, castHeight))
        {
            var d = Vector3.Dot(hit.ComputeNormal(), Vector3.UnitY);
            if (d >= 0.7f)
            {
                draftMarkers[waymark] = hit.Point.Round();
                return;
            }
        }
        ClearDraftMarker(waymark);
    }
    internal void ClearDraftMarker(Waymark waymark)
    {
        draftMarkers.Remove(waymark);
    }

    internal bool IsPossibleToNativePlace()
    {
        return NativePresetPlacementStatus() is PlacementUnsafeReason.Safe;
    }

    internal PlacementUnsafeReason NativePresetPlacementStatus()
    {
        var status = GeneralWaymarkPlacementStatus();
        if (status != PlacementUnsafeReason.Safe)
            return status;
        if (!territoryInfo.ArePresetsSupported)
            return PlacementUnsafeReason.UnsupportedContentType;
        return PlacementUnsafeReason.Safe;
    }

    public void AdjustPresetHeight(WaymarkPreset preset, float castHeight = 20f)
    {
        if (!preset.IsCompatibleTerritory(territoryId)) return;
        if (!Plugin.WaymarkManager.IsPlayerWithinTraceDistance(preset)) return;
        foreach (Waymark w in Enum.GetValues<Waymark>())
        {
            if (preset.PendingHeightAdjustment.IsSet(w)
                && preset.MarkerPositions.TryGetValue(w, out Vector3 p))
            {
                p.Y = Plugin.ObjectTable.LocalPlayer!.Position.Y;
                if (Raycaster.CheckAndSnapY(ref p, castHeight: castHeight))
                {
                    preset.MarkerPositions[w] = p.Round();
                    preset.PendingHeightAdjustment.Set(w, false);
                }
            }
        }
    }

    internal enum PlacementUnsafeReason
    {
        Safe,
        NoLocalPlayer,
        Occupied,
        InCombat,
        DutyRecorderPlayback,
        UnsupportedContentType,
        TooFar,
        NotGrounded,
        UnsupportedArea,
    }
}
