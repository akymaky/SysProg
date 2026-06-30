using System.Net;

namespace _02_34_SysProg;

public class WorkerPool(
    int poolSize,
    RequestQueue<HttpListenerContext> queue,
    RequestHandler handler,
    CancellationToken ct)
{
    private readonly List<Task> _tasks = new();

    public Task StartAsync()
    {
        for (var i = 0; i < poolSize; i++)
        {
            var task = Task.Run(async () =>
            {
                await foreach (var ctx in queue.DequeueAllAsync(ct))
                    try
                    {
                        await handler.HandleAsync(ctx, ct);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Worker error: " + ex.Message);
                        await SafeWriteError(ctx, 500, "Internal server error.");
                    }
            }, ct);

            task.ContinueWith(t => { Logger.Error($"Worker task faulted: {t.Exception?.InnerException?.Message}"); },
                TaskContinuationOptions.OnlyOnFaulted);

            _tasks.Add(task);
        }

        return Task.WhenAll(_tasks);
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