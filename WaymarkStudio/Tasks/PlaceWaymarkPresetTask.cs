using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Threading;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace WaymarkStudio.Tasks;
internal class PlaceWaymarkPresetTask : RetriableTaskBase
{
    private WaymarkPreset preset;

    public static Task Start(CancellationTokenSource cancelToken, WaymarkPreset preset, bool rethrow = false)
    {
        return new PlaceWaymarkPresetTask(preset).StartTask(cancelToken, rethrow);
    }
    private PlaceWaymarkPresetTask(WaymarkPreset preset)
    {
        this.preset = preset;
    }

    private unsafe byte PlacePresetNative(WaymarkPreset preset)
    {
        // TODO Does this do anything or does client do it for me?
        if (Plugin.WaymarkManager.Waymarks.Equals(preset.MarkerPositions))
            return 0;
        var placementStruct = preset.ToMarkerPresetPlacement();
        return NativeFunctions.PlacePreset(MarkingController.Instance(), &placementStruct);
    }

    private async Task PlacePresetPolyfill(WaymarkPreset preset)
    {
        List<Waymark> needsPlace = new();
        List<Waymark> needsClear = new();
        List<Waymark> unmoved = new();

        var waymarks = Plugin.WaymarkManager.Waymarks;
        foreach (Waymark w in Enum.GetValues<Waymark>())
        {
            var hasNew = preset.MarkerPositions.TryGetValue(w, out var newPos);
            var hasOld = waymarks.TryGetValue(w, out var oldPos);

            bool isNew = hasNew && !hasOld;
            bool isMoved = hasNew == hasOld == true && newPos != oldPos;
            bool isOld = hasOld && !hasNew;

            if (isNew || isMoved)
                needsPlace.Add(w);
            else if (isOld)
                needsClear.Add(w);
            else
                unmoved.Add(w);
        }

        if (needsClear.Count > unmoved.Count)
        {
            CheckIfCancelled();
            await ClearWaymarksTask.Start(cancelToken, rethrow: true);
            await Plugin.Framework.DelayTicks(WaymarkRetryTickDelay);
            needsPlace.AddRange(unmoved);
        }
        else
        {
            foreach (Waymark w in needsClear)
            {
                CheckIfCancelled();
                await ClearWaymarkTask.Start(cancelToken, w, rethrow: true);
                await Plugin.Framework.DelayTicks(Plugin.Config.WaymarkPlacementFrequency);
            }
            foreach (Waymark w in unmoved)
                Plugin.WaymarkManager.ClearDraftMarker(w);
        }

        foreach (Waymark w in needsPlace)
        {
            CheckIfCancelled();
            await PlaceWaymarkTask.Start(cancelToken, w, preset.MarkerPositions[w], rethrow: true);
            await Plugin.Framework.DelayTicks(Plugin.Config.WaymarkPlacementFrequency);
        }
    }

    private async Task PlaceWaymarksWithMinimalOperations(WaymarkPreset preset)
    {
        if (preset.MarkerPositions.Count == 0) return;
        if (!preset.IsCompatibleTerritory(Plugin.ClientState.TerritoryType)) return;
        if (preset.PendingHeightAdjustment.IsAnySet()) return;

        if (Plugin.WaymarkManager.IsPossibleToNativePlace())
        {
            var status = PlacePresetNative(preset);
            if (status == 0)
            {
                Plugin.WaymarkManager.ClearDraftMarkers();
                return;
            }
            throw new InvalidOperationException($"Place waymark preset failed status: {status}");
        }

        await PlacePresetPolyfill(preset);
    }

    internal override async Task BeginAsyncRetriableOperation()
    {
        await PlaceWaymarksWithMinimalOperations(preset);
    }

    internal override void OnTaskSuccess()
    {
        Plugin.WaymarkManager.ClearDraftMarkers();
    }
}
