using System.Collections.Concurrent;
using System.Net;

namespace _02_34_SysProg;

public class WorkerPool(
    int maxConcurrency,
    RequestQueue<HttpListenerContext> queue,
    RequestHandler handler,
    CancellationToken ct)
{
    private readonly ConcurrentDictionary<Task, byte> _runningTasks = new();
    private readonly SemaphoreSlim _concurrencyLimiter = new(maxConcurrency);

    public async Task StartAsync()
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var ctx = await queue.DequeueAsync(ct);
                
                await _concurrencyLimiter.WaitAsync(ct);

                var task = Task.Run(async () =>
                {
                    try
                    {
                        await handler.HandleAsync(ctx, ct);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Worker error: " + ex.Message);
                        await SafeWriteError(ctx, 500, "Internal server error.");
                    }
                    finally
                    {
                        _concurrencyLimiter.Release();
                    }
                }, ct);
                
                _runningTasks.TryAdd(task, 0);
                
                task.ContinueWith(completedTask =>
                {
                    _runningTasks.TryRemove(completedTask, out _);

                    if (completedTask.IsFaulted)
                        Logger.Error($"Worker task faulted: {completedTask.Exception?.InnerException?.Message}");
                }, TaskContinuationOptions.ExecuteSynchronously);
            }
        }
        catch (OperationCanceledException)
        {
            Logger.Info("Worker pool cancelled.");
        }
        finally
        {
            await Task.WhenAll(_runningTasks.Keys);
        }
    }

    private static async Task SafeWriteError(HttpListenerContext ctx, int code, string message)
    {
        try
        {
            ctx.Response.StatusCode = code;
            await using var writer = new StreamWriter(ctx.Response.OutputStream);
            await writer.WriteAsync(message);
        }
        finally
        {
            try
            {
                ctx.Response.Close();
            }
            catch
            {
                // ignored
            }
        }
    }
}