using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using static WaymarkStudio.WaymarkManager;

namespace WaymarkStudio.Tasks;
internal class PlaceWaymarkTask : RetriableTaskBase
{
    private Waymark waymark;
    private Vector3 position;

    public static Task Start(CancellationTokenSource cancelToken, Waymark waymark, Vector3 position, bool rethrow = false)
    {
        return new PlaceWaymarkTask(waymark, position).StartTask(cancelToken, rethrow);
    }
    private PlaceWaymarkTask(Waymark waymark, Vector3 position)
    {
        this.waymark = waymark;
        this.position = position;
    }

    private unsafe static byte PlaceWaymark(Waymark waymark, Vector3 newPosition)
    {
        if (Plugin.WaymarkManager.Waymarks.TryGetValue(waymark, out var currPosition) && newPosition == currPosition)
            return 0;
        return NativeFunctions.PlaceWaymark(waymark, newPosition);
    }
    
    internal override Task BeginAsyncRetriableOperation()
    {
        var reason = Plugin.WaymarkManager.WaymarkPlacementStatus(position);
        if (reason is PlacementUnsafeReason.Safe)
        {
            var status = PlaceWaymark(waymark, position);
            if (status == 0)
            {
                Plugin.WaymarkManager.ClearDraftMarker(waymark);
                return Task.CompletedTask;
            }
            return Task.FromException(new InvalidOperationException($"Waymark placement failed status: {status}"));
        }
        else if (reason is PlacementUnsafeReason.NotGrounded or PlacementUnsafeReason.TooFar)
            return Task.FromException(new InvalidOperationException($"Waymark placement unsafe: {reason}\nYou can disable this check in the config."));
        // Could not place for some expected reason; swallow the error.
        return Task.CompletedTask;
    }
}
