using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using Lumina.Excel.Sheets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    private static readonly TimeSpan MinPlacementFrequency = TimeSpan.FromSeconds(0.25);

    internal bool showGuide = false;
    internal Guide guide;
    internal ushort territoryId;
    internal ushort contentFinderId;
    internal ContentType contentType;
    internal string mapName;
    internal Dictionary<Waymark, Vector3> placeholders = new();
    internal IReadOnlyDictionary<Waymark, Vector3> hoverPreviews = EmptyWaymarks;
    private readonly Stopwatch lastPlacementTimer;
    private List<(Waymark waymark, Vector3 wPos)> safePlaceQueue = new();

    private unsafe delegate byte PlaceWaymark(MarkingController* markingController, uint marker, Vector3 wPos);
    private readonly PlaceWaymark placeWaymarkFn;

    private unsafe delegate byte ClearWaymark(MarkingController* markingController, uint marker);
    private readonly ClearWaymark clearWaymarkFn;

    private unsafe delegate byte ClearWaymarks(MarkingController* markingController);
    private readonly ClearWaymarks clearWaymarksFn;

    private unsafe delegate byte WaymarkSafety();
    private readonly WaymarkSafety waymarkSafetyFn;

    private unsafe delegate byte PlacePreset(MarkingController* markingController, MarkerPresetPlacement* placement);
    private readonly PlacePreset placePresetFn;

    internal WaymarkPreset DraftPreset { get { return new(mapName, territoryId, contentFinderId, new Dictionary<Waymark, Vector3>(placeholders)); } }
    internal WaymarkPreset WaymarkPreset { get { return new(mapName, territoryId, contentFinderId, new Dictionary<Waymark, Vector3>(Waymarks)); } }

    internal IReadOnlyDictionary<Waymark, Vector3> Placeholders => placeholders;
    internal IReadOnlyDictionary<Waymark, Vector3> HoverPreviews => hoverPreviews;
    internal unsafe IReadOnlyDictionary<Waymark, Vector3> Waymarks => MarkingController.Instance()->ActiveMarkers();

    public WaymarkManager()
    {
        lastPlacementTimer = new();
        lastPlacementTimer.Start();
        placeWaymarkFn = Marshal.GetDelegateForFunctionPointer<PlaceWaymark>(Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 84 C0 0F 85 ?? ?? ?? ?? EB 23"));
        clearWaymarkFn = Marshal.GetDelegateForFunctionPointer<ClearWaymark>(Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? EB D8 83 FB 09"));
        clearWaymarksFn = Marshal.GetDelegateForFunctionPointer<ClearWaymarks>(Plugin.SigScanner.ScanText("41 55 48 83 EC 50 4C 8B E9"));
        waymarkSafetyFn = Marshal.GetDelegateForFunctionPointer<WaymarkSafety>(Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 84 C0 74 0D B0 05"));
        placePresetFn = Marshal.GetDelegateForFunctionPointer<PlacePreset>(Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? 84 C0 75 1B B0 01"));
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
        return clearWaymarkFn.Invoke(MarkingController.Instance(), (uint)waymark);
    }
    internal unsafe byte NativeClearWaymarks()
    {
        if (Waymarks.Count == 0) return 0;
        return clearWaymarksFn.Invoke(MarkingController.Instance());
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

    internal bool SafePlaceWaymark(Waymark waymark, Vector3 wPos, bool retryError = false)
    {
        var reason = WaymarkPlacementStatus(wPos);
        if (reason == PlacementUnsafeReason.Safe)
        {
            lastPlacementTimer.Restart();
            var status = UnsafeNativePlaceWaymark(waymark, wPos);
            if (status == 0) return true;

            // Too frequent
            if (retryError && status == 2)
            {
                // TODO just place everything through the queue?
                safePlaceQueue.Add((waymark, wPos));
                processSafePlaceQueue(false);
                return true;
            }
            if (status != 2)
                Plugin.Chat.PrintError("Native placement failed with status " + status, Plugin.Tag);
        }

        return false;
    }

    internal unsafe bool IsWaymarksEnabled()
    {
        return waymarkSafetyFn.Invoke() == 0;
    }

    private unsafe byte UnsafeNativePlaceWaymark(Waymark waymark, Vector3 wPos)
    {
        return placeWaymarkFn?.Invoke(MarkingController.Instance(), (uint)waymark, wPos) ?? 69;
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

    public void SafePlacePreset(WaymarkPreset preset, bool clearPlaceholder = true, bool mergeNative = false)
    {
        if (preset.MarkerPositions.Count == 0) return;
        if (preset.TerritoryId != territoryId) return;
        if (preset.PendingHeightAdjustment.IsAnySet()) return;
        if (IsPossibleToNativePlace())
        {
            if (mergeNative)
                foreach ((Waymark w, Vector3 p) in Plugin.WaymarkManager.Waymarks)
                    if (!preset.MarkerPositions.ContainsKey(w))
                        preset.MarkerPositions.Add(w, p);

            UnsafeNativePlacePreset(preset.ToGamePreset());
            if (clearPlaceholder) Plugin.WaymarkManager.ClearPlaceholders();
        }
        else
        {
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
                    safePlaceQueue.Add((w, wPos));
                }
            }
            processSafePlaceQueue(clearPlaceholder);
        }
    }

    internal async void processSafePlaceQueue(bool clearPlaceholder = true)
    {
        await Plugin.Framework.Run(async () =>
        {
            var territoryId = this.territoryId;
            int attempts = safePlaceQueue.Count + 2;
            while (safePlaceQueue.Count > 0 && territoryId == this.territoryId && attempts-- > 0)
            {
                (Waymark waymark, Vector3 wPos) = safePlaceQueue[0];
                safePlaceQueue.RemoveAt(0);
                if (SafePlaceWaymark(waymark, wPos))
                {
                    if (clearPlaceholder && placeholders.GetValueOrDefault(waymark) == wPos)
                        placeholders.Remove(waymark);
                }
                else
                {
                    // requeue failure in case it was retriable
                    // TODO actually differentiate between retriable and not
                    safePlaceQueue.Add((waymark, wPos));
                }
                await Plugin.Framework.DelayTicks(50);
            }
            safePlaceQueue.Clear();
            // Plugin.Chat.Print("Placement complete");
        });
    }

    private unsafe bool UnsafeNativePlacePreset(FieldMarkerPreset preset)
    {
        var placementStruct = preset.ToMarkerPresetPlacement();
        var status = placePresetFn.Invoke(MarkingController.Instance(), &placementStruct);
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
        InCombat,
        DutyRecorderPlayback,
        UnsupportedContentType,
        NoWaymarksPlaced,
        TooFar,
        NotGrounded,
        TooFrequent,
        UnsupportedArea,
    }
}
