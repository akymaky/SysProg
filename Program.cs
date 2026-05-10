using System.Text;
using _01_34_SysProg;

var router = new GifRouter();

router.Get("/test", async ctx =>
{
    string body = $"Hello!";
    byte[] data = Encoding.UTF8.GetBytes(body);
    ctx.Response.ContentType = "text/plain";
    ctx.Response.ContentLength64 = data.Length;
    await ctx.Response.OutputStream.WriteAsync(data, 0, data.Length);
});

using var api = new GifApi("http://localhost:8080/", router.TryRouteAsync);
api.Start();

Console.WriteLine("Press ENTER to stop...");
Console.ReadLine();
api.Stop();