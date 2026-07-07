using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using _03_34_SysProg.Messages;
using _03_34_SysProg.Models;
using Akka.Actor;
using Serilog;

namespace _03_34_SysProg.Rx;

public sealed class HttpServer : IDisposable
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly HttpListener _listener = new();
    private readonly IActorRef _queryTarget;
    private bool _disposed;
    private IDisposable? _subscription;

    public HttpServer(string prefix, IActorRef queryTarget)
    {
        _listener.Prefixes.Add(prefix);
        _queryTarget = queryTarget;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Stop();
        _listener.Close();
        _subscription?.Dispose();
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _listener.Start();
        Log.Information("Started HTTP server on {Prefix}", _listener.Prefixes);

        _subscription = Observable
            .FromAsync(() => _listener.GetContextAsync())
            .Repeat()
            .ObserveOn(Scheduler.Default)
            .Subscribe(
                async ctx => await HandleAsync(ctx),
                ex => Log.Error(ex, "Error handling HTTP request")
            );
    }

    private async Task HandleAsync(HttpListenerContext ctx)
    {
        var request = ctx.Request;
        var response = ctx.Response;
        var requestId = Guid.NewGuid().ToString("N")[..8];

        var startTime = DateTime.Now;

        Log.Information(
            "[{RequestId}] {Client} >> HTTP {Method} {Url}",
            requestId,
            request.RemoteEndPoint,
            request.HttpMethod,
            request.Url
        );

        var analysisResponse = await _queryTarget.Ask<AnalysisResult>(
            new GetCurrentState(NytPeriod.Day)
        );

        var json = JsonSerializer.Serialize(analysisResponse, _jsonOptions);

        response.StatusCode = 200;
        response.ContentType = "application/json; charset=utf-8";

        var buffer = Encoding.UTF8.GetBytes(json);
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);

        ctx.Response.StatusCode = 200;
        ctx.Response.Close();

        Log.Information(
            "[{RequestId}] {Client} << Request handled in {Elapsed}",
            requestId,
            request.RemoteEndPoint,
            DateTime.Now - startTime
        );
    }

    private void Stop()
    {
        if (_disposed) return;
        _listener.Stop();
        Log.Information("Stopping HTTP server");
    }
}