using System.Net;
using System.Text;

namespace _02_34_SysProg;

public class RequestHandler(SearchService searchService, TtlCache<GifCacheItem> cache)
{
    public async Task HandleAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        var reqPath = ctx.Request.Url!.AbsolutePath.Trim('/');
        var fileName = Path.GetFileName(reqPath);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            await WriteTextAsync(ctx, 400, "File name is missing.", ct);
            return;
        }

        if (!string.Equals(Path.GetExtension(fileName), ".gif", StringComparison.OrdinalIgnoreCase))
        {
            await WriteTextAsync(ctx, 400, "Only GIF files are supported.", ct);
            return;
        }

        Logger.Info($"Request: {fileName}");

        var cacheKey = fileName.ToLowerInvariant();

        GifCacheItem? cached;
        try
        {
            cached = await cache.GetOrAddAsync(cacheKey, async () =>
            {
                var fullPath = searchService.FindFile(fileName);

                if (fullPath == null || !File.Exists(fullPath)) return null;

                var bytes = await File.ReadAllBytesAsync(fullPath, ct);
                return new GifCacheItem(fullPath, bytes);
            });
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to get cached item for {fileName}: {ex.Message}");
            await WriteTextAsync(ctx, 500, "Internal server error.", ct);
            return;
        }

        if (cached == null)
        {
            await WriteTextAsync(ctx, 404, $"File with name '{fileName}' does not exist.", ct);
            return;
        }

        try
        {
            var data = cached.data;
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "image/gif";
            ctx.Response.ContentLength64 = data.Length;

            var dataTask = Task.FromResult(data);

            var writeTask = dataTask.ContinueWith(t =>
            {
                var bytes = t.Result;
                return ctx.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length, ct);
            }, ct, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);

            writeTask.ContinueWith(t => { Logger.Info($"Served: {fileName} ({data.Length} bytes)"); },
                TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);

            writeTask.ContinueWith(t =>
            {
                if (t.IsFaulted) Logger.Error($"Failed to write response for {fileName}: {t.Exception.InnerExceptions}");
            }, TaskContinuationOptions.OnlyOnFaulted);

            await writeTask;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to write response for {fileName}: {ex.Message}");
        }
        finally
        {
            try
            {
                ctx.Response.Close();
            }
            catch
            {
                /* ignore */
            }
        }
    }

    private static async Task WriteTextAsync(HttpListenerContext ctx, int statusCode, string message,
        CancellationToken ct)
    {
        var data = Encoding.UTF8.GetBytes(message);
        ctx.Response.StatusCode = statusCode;
        ctx.Response.ContentType = "text/plain; charset=utf-8";
        ctx.Response.ContentLength64 = data.Length;
        await ctx.Response.OutputStream.WriteAsync(data.AsMemory(0, data.Length), ct);
        ctx.Response.Close();
    }
}