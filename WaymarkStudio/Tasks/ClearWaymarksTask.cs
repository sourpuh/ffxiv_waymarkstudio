using FFXIVClientStructs.FFXIV.Client.Game.UI;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WaymarkStudio.Tasks;
internal class ClearWaymarksTask : RetriableTaskBase
{
    public static Task Start(CancellationTokenSource cancelToken, bool rethrow = false)
    {
        return new ClearWaymarksTask().StartTask(cancelToken, rethrow);
    }

    private unsafe static byte ClearWaymarks()
    {
        if (Plugin.WaymarkManager.Waymarks.Count == 0)
            return 0;
        return NativeFunctions.ClearWaymarks(MarkingController.Instance());
    }

    internal override Task BeginAsyncRetriableOperation()
    {
        var status = ClearWaymarks();
        if (status == 0)
            return Task.CompletedTask;
        return Task.FromException(new InvalidOperationException($"Clear all waymarks failed status: {status}"));
    }

    internal override void OnTaskComplete()
    {
        // Do nothing
    }
}
