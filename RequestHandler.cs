using System.Net;
using System.Text;

namespace _01_34_SysProg;

public class RequestHandler(SearchService searchService, TtlCache<GifCacheItem> cache)
{

    public void Handle(HttpListenerContext ctx)
    {
        var reqPath = ctx.Request.Url!.AbsolutePath.Trim('/');
        var fileName = Path.GetFileName(reqPath);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            WriteText(ctx, 400, "File name is missing.");
            return;
        }

        if (!string.Equals(Path.GetExtension(fileName), ".gif", StringComparison.OrdinalIgnoreCase))
        {
            WriteText(ctx, 400, "Only GIF files are supported.");
            return;
        }

        Logger.Info($"Request: {fileName}");
        
        var cacheKey = fileName.ToLowerInvariant();

        GifCacheItem? cached = cache.GetOrAdd(cacheKey, () => {
            string? fullPath = searchService.FindFile(fileName);

            if (fullPath == null || !File.Exists(fullPath))
            {
                return null;
            }

            byte[] bytes = File.ReadAllBytes(fullPath);
            return new GifCacheItem(fullPath, bytes);
        });

        if (cached == null)
        {
            WriteText(ctx, 404, $"File with name '{fileName}' does not exist.");
            return;
        }

        try
        {
            byte[] data = cached.Data;
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "image/gif";
            ctx.Response.ContentLength64 = data.Length;
            ctx.Response.OutputStream.Write(data, 0, data.Length);
            Logger.Info($"Served: {fileName} ({data.Length} bytes)");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to write response for {fileName}: {ex.Message}");
        }
        finally
        {
            try { ctx.Response.Close(); } catch { /* ignore */ }
        }
    }

    private void WriteText(HttpListenerContext ctx, int statusCode, string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        ctx.Response.StatusCode = statusCode;
        ctx.Response.ContentType = "text/plain; charset=utf-8";
        ctx.Response.ContentLength64 = data.Length;
        ctx.Response.OutputStream.Write(data, 0, data.Length);
        ctx.Response.Close();
    }
}