using System.Net;
using System.Text;

namespace _01_34_SysProg;

class HttpServer : IDisposable
{
    private readonly HttpListener _listener;
    private readonly RequestQueue<HttpListenerContext> _queue;
    private readonly CancellationTokenSource _cts = new();
    
    public CancellationToken Token => _cts.Token;

    public HttpServer(string prefix, RequestQueue<HttpListenerContext> queue)
    {
        if (!HttpListener.IsSupported)
        {
            throw new PlatformNotSupportedException("HttpListener is not supported on this platform!");
        }
        
        _listener = new HttpListener();
        _listener.Prefixes.Add(prefix);
        
        _queue = queue;
    }

    public void Start()
    {
        _listener.Start();
        Console.WriteLine($"Listening on {String.Join(", ", _listener.Prefixes)}");
        
        var thread = new Thread(ListenLoop)
        {
            IsBackground = true,
            Name = "ListenerThread"
        };
        
        thread.Start();
        Console.WriteLine($"Server thread started.");
        
    }

    private void ListenLoop()
    {
        try
        {
            while (!Token.IsCancellationRequested)
            {
                var ctx = _listener.GetContext();
                _queue.Enqueue(ctx);
            }
        }
        catch (HttpListenerException ex) when (Token.IsCancellationRequested)
        {
            Console.WriteLine("Listener stopped due to cancellation");
        }
        catch (ObjectDisposedException) when (Token.IsCancellationRequested)
        {
            Console.WriteLine("Listener disposed while shutting down");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Listener error: {ex}");
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