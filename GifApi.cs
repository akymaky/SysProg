using System.Net;
using System.Text;

namespace _01_34_SysProg;

class GifApi : IDisposable
{
    private readonly HttpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly Func<HttpListenerContext, Task> _requestHandler;

    public GifApi(string prefix, Func<HttpListenerContext, Task> handler)
    {
        if (!HttpListener.IsSupported)
        {
            throw new PlatformNotSupportedException("HttpListener is not supported on this platform!");
        }
        
        _listener = new HttpListener();
        _listener.Prefixes.Add(prefix);
        _requestHandler = handler ??  throw new ArgumentNullException(nameof(handler));
    }

    public void Start()
    {
        _listener.Start();
        Console.WriteLine($"Listening on {String.Join(", ", _listener.Prefixes)}");
        _ = AcceptLoopAsync(_cts.Token);
    }

    private async Task AcceptLoopAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                HttpListenerContext ctx = await _listener.GetContextAsync();

                ThreadPool.QueueUserWorkItem(_ => ProcessRequestAsync(ctx), null);
            }
        }
        catch (HttpListenerException ex) when (_cts.IsCancellationRequested)
        {
            Console.WriteLine("Request was cancelled");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Accept error: {ex}");
        }
    }

    private async void ProcessRequestAsync(HttpListenerContext ctx)
    {
        try
        {
            await _requestHandler(ctx);
        }
        catch (Exception ex)
        {
            ctx.Response.StatusCode = 500;
            byte[] buf = Encoding.UTF8.GetBytes($"Internal server error: {ex.Message}");
            await ctx.Response.OutputStream.WriteAsync(buf, 0, buf.Length);
        }
        finally
        {
            ctx.Response.OutputStream.Close();
        }
    }

    public void Stop()
    {
        _cts.Cancel();
        _listener.Stop();
        _listener.Close();
    }
    
    public void Dispose()
    {
        Stop();
    }
}