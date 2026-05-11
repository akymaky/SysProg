using System.Text;
using _01_34_SysProg;

ManualResetEvent shutdownEvent = new ManualResetEvent(false);

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

Console.CancelKeyPress += (sender, eventArgs) =>
{
    eventArgs.Cancel = true;
    Console.WriteLine("Shutting down...");
    shutdownEvent.Set();
};

Console.WriteLine("Press CTRL+C to stop.");

shutdownEvent.WaitOne();

api.Stop();