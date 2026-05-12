using System.Net;
using System.Text;

namespace _01_34_SysProg;

public class RequestHandler(SearchService searchService, TtlCache cache)
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

        Logger.Info($"Request: {fileName}");

        string? fullPath = cache.GetOrAdd(fileName, () => searchService.FindFile(fileName));

        if (fullPath == null || !File.Exists(fullPath))
        {
            WriteText(ctx, 404, $"File with name '{fileName}' does not exist.");
            return;
        }

        byte[] data = File.ReadAllBytes(fullPath);

        ctx.Response.StatusCode = 200;
        ctx.Response.ContentType = GetContentType(fullPath);
        ctx.Response.ContentLength64 = data.Length;
        ctx.Response.OutputStream.Write(data, 0, data.Length);
        ctx.Response.Close();

        Logger.Info($"Served: {fileName} ({data.Length} bytes)");
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

    private string GetContentType(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".gif" => "image/gif",
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };
    }
    
}