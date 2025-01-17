using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using WaymarkStudio.Guides;
using Guide = WaymarkStudio.Guides.Guide;

namespace WaymarkStudio;
/**
 * Manager for drafting and interacting with native waymarks.
 */
internal class WaymarkManager
{
    private static readonly IReadOnlyDictionary<Waymark, Vector3> EmptyWaymarks = new Dictionary<Waymark, Vector3>();

    internal bool showGuide = false;
    internal Guide guide;
    internal ushort territoryId;
    internal ushort contentFinderId;
    internal ContentType contentType;
    internal string mapName;
    internal Dictionary<Waymark, Vector3> placeholders = new();
    internal IReadOnlyDictionary<Waymark, Vector3> hoverPreviews = EmptyWaymarks;
    private Queue<(Waymark waymark, Vector3 wPos)> safePlaceQueue = new();

#pragma warning disable IDE1006 // Naming Styles
    private unsafe delegate byte PlaceWaymarkDelegate(MarkingController* markingController, uint marker, Vector3 wPos);
    private readonly PlaceWaymarkDelegate PlaceWaymark;

    private unsafe delegate byte ClearWaymarkDelegate(MarkingController* markingController, uint marker);
    private readonly ClearWaymarkDelegate ClearWaymarkFn;

    private unsafe delegate byte ClearWaymarksDelegate(MarkingController* markingController);
    private readonly ClearWaymarksDelegate ClearWaymarks;

    private unsafe delegate byte WaymarkSafetyDelegate();
    private readonly WaymarkSafetyDelegate WaymarkSafety;

    private unsafe delegate byte PlacePresetDelegate(MarkingController* markingController, MarkerPresetPlacement* placement);
    private readonly PlacePresetDelegate PlacePreset;
#pragma warning restore IDE1006 // Naming Styles

    internal WaymarkPreset DraftPreset { get { return new(mapName, territoryId, contentFinderId, new Dictionary<Waymark, Vector3>(placeholders)); } }
    internal WaymarkPreset WaymarkPreset { get { return new(mapName, territoryId, contentFinderId, new Dictionary<Waymark, Vector3>(Waymarks)); } }

    internal IReadOnlyDictionary<Waymark, Vector3> Placeholders => placeholders;
    internal IReadOnlyDictionary<Waymark, Vector3> HoverPreviews => hoverPreviews;
    internal unsafe IReadOnlyDictionary<Waymark, Vector3> Waymarks => MarkingController.Instance()->ActiveMarkers();

    public WaymarkManager()
    {
        PlaceWaymark = Marshal.GetDelegateForFunctionPointer<PlaceWaymarkDelegate>(Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 84 C0 0F 85 ?? ?? ?? ?? EB 23"));
        ClearWaymarkFn = Marshal.GetDelegateForFunctionPointer<ClearWaymarkDelegate>(Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? EB D8 83 FB 09"));
        ClearWaymarks = Marshal.GetDelegateForFunctionPointer<ClearWaymarksDelegate>(Plugin.SigScanner.ScanText("41 55 48 83 EC 50 4C 8B E9"));
        WaymarkSafety = Marshal.GetDelegateForFunctionPointer<WaymarkSafetyDelegate>(Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 84 C0 74 0D B0 05"));
        PlacePreset = Marshal.GetDelegateForFunctionPointer<PlacePresetDelegate>(Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 84 C0 75 1B B0 01"));
    }

    internal void OnTerritoryChange(TerritoryType territory)
    {
        territoryId = (ushort)territory.RowId;
        mapName = territory.PlaceName.Value.Name.ExtractText();
        contentFinderId = (ushort)territory.ContentFinderCondition.RowId;
        contentType = 0;
        if (contentFinderId != 0)
        {
            mapName = territory.ContentFinderCondition.Value.Name.ExtractText();
            contentType = (ContentType)territory.ContentFinderCondition.Value.ContentType.RowId;
        }
        placeholders.Clear();
        hoverPreviews = EmptyWaymarks;
        showGuide = false;
        guide = new CircleGuide(radius: 1);
        safePlaceQueue.Clear();
    }
    internal void ClearPlaceholders()
    {
        placeholders.Clear();
    }
    internal unsafe byte NativeClearWaymark(Waymark waymark)
    {
        if (!Waymarks.ContainsKey(waymark)) return 0;
        return ClearWaymarkFn(MarkingController.Instance(), (uint)waymark);
    }
    internal unsafe byte NativeClearWaymarks()
    {
        if (Waymarks.Count == 0) return 0;
        return ClearWaymarks(MarkingController.Instance());
    }

    public bool IsPlayerWithinTraceDistance(WaymarkPreset preset)
    {
        return preset.MaxDistanceTo(Plugin.ClientState.LocalPlayer.Position) < 200;
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
        if (Raycaster.Raycast(castOrigin, -Vector3.UnitY, out RaycastHit hitInfo, castHeight))
        {
            return point.Y == hitInfo.Point.Round().Y;
        }
        return false;
    }

    internal PlacementUnsafeReason GeneralWaymarkPlacementStatus()
    {
        if (Plugin.ClientState.LocalPlayer == null)
            return PlacementUnsafeReason.NoLocalPlayer;
        if (!IsWaymarksEnabled())
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
               || Condition[ConditionFlag.Crafting40]
               || Condition[ConditionFlag.PreparingToCraft]
               || Condition[ConditionFlag.InThatPosition]
               || Condition[ConditionFlag.Unconscious]
               || Condition[ConditionFlag.MeldingMateria]
               || Condition[ConditionFlag.Gathering]
               || Condition[ConditionFlag.OperatingSiegeMachine]
               || Condition[ConditionFlag.CarryingItem]
               || Condition[ConditionFlag.CarryingObject]
               || Condition[ConditionFlag.BeingMoved]
               || Condition[ConditionFlag.Mounted2]
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
               || Plugin.ClientState.LocalPlayer?.IsTargetable != true;
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
        if (Raycaster.Raycast(castOrigin, -Vector3.UnitY, out RaycastHit hit, castHeight))
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

    public bool SafePlaceWaymark(Waymark waymark, Vector3 wPos)
    {
        var reason = WaymarkPlacementStatus(wPos);
        if (reason is PlacementUnsafeReason.Safe)
        {
            var status = UnsafeNativePlaceWaymark(waymark, wPos);
            if (status == 0) return true;

            // Retry Too frequent
            if (status == 2)
            {
                safePlaceQueue.Enqueue((waymark, wPos));
                processSafePlaceQueue(clearPlaceholder: false);
                return true;
            }
            if (status != 2)
                Plugin.Chat.PrintError("Native placement failed with status " + status, Plugin.Tag);
        }

        return false;
    }
    private unsafe byte UnsafeNativePlaceWaymark(Waymark waymark, Vector3 wPos)
    {
        return PlaceWaymark(MarkingController.Instance(), (uint)waymark, wPos);
    }

    internal unsafe bool IsWaymarksEnabled()
    {
        return WaymarkSafety() == 0;
    }

    internal bool IsPossibleToNativePlace()
    {
        return NativePresetPlacementStatus() is PlacementUnsafeReason.Safe or PlacementUnsafeReason.NoWaymarksPlaced;
    }

    internal PlacementUnsafeReason NativePresetPlacementStatus()
    {
        var status = GeneralWaymarkPlacementStatus();
        if (status != PlacementUnsafeReason.Safe)
            return status;
        if (!(contentType is
            ContentType.Dungeons
            or ContentType.Guildhests
            or ContentType.Trials
            or ContentType.Raids
            or ContentType.UltimateRaids
            or ContentType.SavetheQueen
            or ContentType.VCDungeonFinder
            or ContentType.ChaoticAllianceRaid))
            return PlacementUnsafeReason.UnsupportedContentType;
        if (contentType is ContentType.SavetheQueen && contentFinderId is not /*DR*/760 or /*DRS*/761)
            return PlacementUnsafeReason.UnsupportedContentType;
        if (Placeholders.Count == 0)
            return PlacementUnsafeReason.NoWaymarksPlaced;
        return PlacementUnsafeReason.Safe;
    }

    public void AdjustPresetHeight(WaymarkPreset preset, float castHeight = 100000f)
    {
        if (preset.TerritoryId != territoryId) return;
        if (!Plugin.WaymarkManager.IsPlayerWithinTraceDistance(preset)) return;
        foreach (Waymark w in Enum.GetValues<Waymark>())
        {
            if (preset.PendingHeightAdjustment.IsSet(w)
                && preset.MarkerPositions.TryGetValue(w, out Vector3 p))
            {
                if (Raycaster.CheckAndSnapY(ref p, castHeight: castHeight))
                {
                    preset.MarkerPositions[w] = p.Round();
                    preset.PendingHeightAdjustment.Set(w, false);
                }
            }
        }
    }

    public void SafePlacePreset(WaymarkPreset preset, bool clearPlaceholder = true, bool mergeExisting = false)
    {
        if (preset.MarkerPositions.Count == 0) return;
        if (preset.TerritoryId != territoryId) return;
        if (preset.PendingHeightAdjustment.IsAnySet()) return;
        if (IsPossibleToNativePlace())
        {
            if (mergeExisting)
                foreach ((Waymark w, Vector3 p) in Plugin.WaymarkManager.Waymarks)
                    if (!preset.MarkerPositions.ContainsKey(w))
                        preset.MarkerPositions.Add(w, p);

            UnsafeNativePlacePreset(preset.ToGamePreset());
            if (clearPlaceholder) Plugin.WaymarkManager.ClearPlaceholders();
        }
        else
        {
            if (!IsSafeToPlaceWaymarks()) return;
            foreach (Waymark w in Enum.GetValues<Waymark>())
            {
                if (preset.MarkerPositions.TryGetValue(w, out var wPos))
                {
                    if (Waymarks.TryGetValue(w, out var existingWPos) && wPos == existingWPos)
                    {
                        if (clearPlaceholder)
                            placeholders.Remove(w);
                        continue;
                    }
                    safePlaceQueue.Enqueue((w, wPos));
                }
                else if (!mergeExisting)
                    NativeClearWaymark(w);
            }
            processSafePlaceQueue(clearPlaceholder);
        }
    }


    private async void processSafePlaceQueue(bool clearPlaceholder = true)
    {
        await Plugin.Framework.Run(async () =>
        {
            var territoryId = this.territoryId;
            int attempts = safePlaceQueue.Count + 2;
            while (safePlaceQueue.Count > 0 && territoryId == this.territoryId && attempts-- > 0)
            {
                (Waymark waymark, Vector3 wPos) = safePlaceQueue.Dequeue();

                var isSafe = Plugin.Config.DisableWorldPresetSafetyChecks || WaymarkPlacementStatus(wPos) is PlacementUnsafeReason.Safe;
                if (!isSafe) continue;

                var status = UnsafeNativePlaceWaymark(waymark, wPos);
                if (status == 0)
                {
                    if (clearPlaceholder && placeholders.GetValueOrDefault(waymark) == wPos)
                        placeholders.Remove(waymark);
                }
                else if (status == 2 || status == 3)
                {
                    // requeue retriable error
                    await Plugin.Framework.DelayTicks(30);
                    safePlaceQueue.Enqueue((waymark, wPos));
                }
                else
                {
                    Plugin.Chat.PrintError("Native placement failed with status " + status, Plugin.Tag);
                }
                await Plugin.Framework.DelayTicks(Plugin.Config.WaymarkPlacementFrequency);
            }
        });
    }

    private unsafe bool UnsafeNativePlacePreset(FieldMarkerPreset preset)
    {
        var placementStruct = preset.ToMarkerPresetPlacement();
        var status = PlacePreset(MarkingController.Instance(), &placementStruct);
        if (status != 0)
        {
            // 7.15 qword_1427C6F00 && (*(_BYTE *)(qword_1427C6F00 + 9) & 8) != 0
            // 7.05 qword_1427316F0 && (*(_BYTE *)(qword_1427316F0 + 9) & 8) != 0
            // returns 6 unsupported area?
            Plugin.Chat.PrintError("Native preset placement failed with status " + status, Plugin.Tag);
        }
        return status == 0;
    }

    internal enum PlacementUnsafeReason
    {
        Safe,
        NoLocalPlayer,
        Occupied,
        InCombat,
        DutyRecorderPlayback,
        UnsupportedContentType,
        NoWaymarksPlaced,
        TooFar,
        NotGrounded,
        UnsupportedArea,
    }
}
