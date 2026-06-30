using System.Net;

namespace _01_34_SysProg;

public class HttpServer : IDisposable
{
    private readonly CancellationToken _ct;
    private readonly HttpListener _listener;
    private readonly RequestQueue<HttpListenerContext> _queue;

    public HttpServer(string prefix, RequestQueue<HttpListenerContext> queue, CancellationToken ct)
    {
        if (!HttpListener.IsSupported)
            throw new PlatformNotSupportedException("HttpListener is not supported on this platform!");

        _listener = new HttpListener();
        _listener.Prefixes.Add(prefix);

        _queue = queue;

        _ct = ct;
    }

    public void Dispose()
    {
        Stop();
    }

    public async Task StartAsync()
    {
        _listener.Start();
        Logger.Info($"Listening on {string.Join(", ", _listener.Prefixes)}");

        try
        {
            while (!_ct.IsCancellationRequested)
            {
                var ctx = await _listener.GetContextAsync();
                await _queue.EnqueueAsync(ctx, _ct);
            }
        }
        catch (HttpListenerException ex) when (_ct.IsCancellationRequested)
        {
            Logger.Error("Listener stopped due to cancellation");
        }
        catch (ObjectDisposedException) when (_ct.IsCancellationRequested)
        {
            Logger.Error("Listener disposed while shutting down");
        }
        catch (OperationCanceledException)
        {
            Logger.Error("Listener cancelled");
        }
        catch (Exception ex)
        {
            Logger.Error($"Listener error: {ex}");
        }
    }

    public void Stop()
    {
        _listener.Stop();
        _listener.Close();
    }
}