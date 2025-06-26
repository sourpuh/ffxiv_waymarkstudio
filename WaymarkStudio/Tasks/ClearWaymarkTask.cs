using FFXIVClientStructs.FFXIV.Client.Game.UI;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WaymarkStudio.Tasks;
internal class ClearWaymarkTask : RetriableTaskBase
{
    private Waymark waymark;

    public static Task Start(CancellationTokenSource cancelToken, Waymark waymark, bool rethrow = false)
    {
        return new ClearWaymarkTask(waymark).StartTask(cancelToken, rethrow);
    }
    private ClearWaymarkTask(Waymark waymark) : base()
    {
        this.waymark = waymark;
    }

    private unsafe static byte ClearWaymark(Waymark waymark)
    {
        if (!Plugin.WaymarkManager.Waymarks.ContainsKey(waymark))
            return 0;
        return NativeFunctions.ClearWaymark(MarkingController.Instance(), (uint)waymark);
    }

    internal override Task BeginAsyncRetriableOperation()
    {
        var status = ClearWaymark(waymark);
        if (status == 0)
            return Task.CompletedTask;
        return Task.FromException(new InvalidOperationException($"Clear waymark failed status: {status}"));
    }

    internal override void OnTaskComplete()
    {
        // Do nothing
    }
}
