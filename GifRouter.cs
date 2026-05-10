using System.Net;

namespace _01_34_SysProg;

public class GifRouter
{
    private readonly Dictionary<string, Func<HttpListenerContext, Task>> _routes = new(StringComparer.OrdinalIgnoreCase);

    public void Get(string path, Func<HttpListenerContext, Task> handler) => Add("GET", path, handler);

    private void Add(string method, string path, Func<HttpListenerContext, Task> handler)
    {
        var key = $"{method}:{NormalizePath(path)}";
        _routes[key] = handler ?? throw new ArgumentNullException(nameof(handler));
    }
    private static string NormalizePath(string p)
    {
        if (!p.StartsWith('/')) p = "/" + p;
        return p.Replace("//", "/");
    }

    public async Task<bool> TryRouteAsync(HttpListenerContext ctx)
    {
        var method = ctx.Request.HttpMethod;
        var rawUrl = ctx.Request.RawUrl;
        if (rawUrl != null)
        {
            var key = $"{method}:{NormalizePath(rawUrl)}";

            if (_routes.TryGetValue(key, out var handler))
            {
                await handler(ctx);
                return true;
            }
        }

        ctx.Response.StatusCode = 404;
        byte[] buf = "Not Found"u8.ToArray();
        await ctx.Response.OutputStream.WriteAsync(buf, 0, buf.Length);
        return false;
    }
}