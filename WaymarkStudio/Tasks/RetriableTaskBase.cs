using System.Threading;
using System.Threading.Tasks;
using System;

namespace WaymarkStudio.Tasks;
internal abstract class RetriableTaskBase
{
    protected const int WaymarkRetryTickDelay = 30;
    protected Task task;
    protected CancellationTokenSource cancelToken;

    internal abstract Task BeginAsyncRetriableOperation();
    internal abstract void OnTaskComplete();

    internal async Task StartTask(CancellationTokenSource cancelToken, bool rethrow = false)
    {
        this.cancelToken = cancelToken;
        task = Plugin.Framework.Run(async () =>
        {
            Exception? lastException = null;
            int attempts = 2;
            while (attempts-- > 0)
            {
                CheckIfCancelled();
                try
                {
                    var task = BeginAsyncRetriableOperation();
                    await task;

                    if (task.IsCompletedSuccessfully || task.IsCanceled)
                        return;
                }
                catch (Exception ex)
                {
                    Plugin.Log.Warning($"{ToString()} | Attempts remaining {attempts} | {ex.Message}");
                    lastException = ex;
                }
                await Plugin.Framework.DelayTicks(WaymarkRetryTickDelay);
            }
            throw new InvalidOperationException($"All attempts failed", lastException);
        });

        try
        {
            await task;
        }
        catch (Exception ex)
        {
            if (rethrow)
                throw;
            else
                Plugin.ReportError(ex);
        }
        finally
        {
            OnTaskComplete();
        }
    }

    internal void Cancel()
    {
        if (IsRunning)
            cancelToken.Cancel();
    }

    internal void CheckIfCancelled()
    {
        cancelToken.Token.ThrowIfCancellationRequested();
    }

    internal bool IsRunning => !task.IsCompleted;
}
