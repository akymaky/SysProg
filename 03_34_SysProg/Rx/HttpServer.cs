using System.Net;
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

        Stop();

        _disposed = true;
        _listener.Close();
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _listener.Start();
        Log.Information("Started HTTP server on {Prefix}", _listener.Prefixes);

        _subscription = Observable
            .FromAsync(() => _listener.GetContextAsync())
            .Repeat()
            .Catch<HttpListenerContext, HttpListenerException>(ex => ex.ErrorCode switch
            {
                995 => Observable.Empty<HttpListenerContext>(),
                _ => Observable.Throw<HttpListenerContext>(ex)
            })
            .Catch<HttpListenerContext, ObjectDisposedException>(_ =>
                Observable.Empty<HttpListenerContext>())
            .SelectMany(ctx => Observable.FromAsync(() => HandleAsync(ctx)))
            .Subscribe(
                _ => Log.Debug("[Rx] HTTP Request processed"),
                ex => Log.Error(ex, "[Rx] HTTP accept fatal error"),
                () => Log.Information("[Rx] HTTP accept loop completed"));
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

        try
        {
            if (request.HttpMethod != "GET")
            {
                await WriteJsonAsync(
                    response,
                    new { error = "Only GET method is supported" },
                    HttpStatusCode.MethodNotAllowed);
                return;
            }

            if (request.Url?.AbsolutePath != "/analysis")
            {
                await WriteJsonAsync(
                    response,
                    new { error = "Not found. Use GET /analysis?period=day|week|month" },
                    HttpStatusCode.NotFound);
                return;
            }

            if (!TryParsePeriod(request.QueryString["period"], out var period))
            {
                await WriteJsonAsync(
                    response,
                    new
                    {
                        error =
                            "Invalid or missing period. Use period=day, period=week, period=month, period=1, period=7 or period=30."
                    },
                    HttpStatusCode.BadRequest);
                return;
            }

            var analysisResult = await _queryTarget.Ask<AnalysisResult>(
                new GetCurrentState(period),
                TimeSpan.FromSeconds(10)
            );

            var analysisResponse = new AnalysisResponse
            {
                Period = period.ToString(),
                TotalArticles = analysisResult.TotalArticles,
                Topics = analysisResult.Topics,
                ProcessedAt = DateTime.UtcNow
            };

            await WriteJsonAsync(response, analysisResponse, HttpStatusCode.OK);

            Log.Information(
                "[{RequestId}] {Client} << Request successfully handled in {Elapsed} for period={Period}",
                requestId,
                request.RemoteEndPoint,
                DateTime.Now - startTime,
                period
            );
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "[{RequestId}] {Client} << Request failed after {Elapsed}",
                requestId,
                request.RemoteEndPoint,
                DateTime.Now - startTime
            );

            try
            {
                await WriteJsonAsync(
                    response,
                    new { error = "Internal server error" },
                    HttpStatusCode.InternalServerError);
            }
            catch
            {
                // ignore
            }
        }
        finally
        {
            response.Close();
        }
    }

    private static bool TryParsePeriod(string? value, out NytPeriod period)
    {
        period = NytPeriod.Day;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.Trim().ToLowerInvariant() switch
        {
            "day" or "1" => SetPeriod(NytPeriod.Day, out period),
            "week" or "7" => SetPeriod(NytPeriod.Week, out period),
            "month" or "30" => SetPeriod(NytPeriod.Month, out period),
            _ => false
        };
    }

    private static bool SetPeriod(NytPeriod value, out NytPeriod period)
    {
        period = value;
        return true;
    }

    private async Task WriteJsonAsync(HttpListenerResponse response, object body, HttpStatusCode statusCode)
    {
        var json = JsonSerializer.Serialize(body, _jsonOptions);
        var buffer = Encoding.UTF8.GetBytes(json);

        response.StatusCode = (int)statusCode;
        response.ContentType = "application/json; charset=utf-8";
        response.ContentLength64 = buffer.Length;

        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
    }

    public void Stop()
    {
        if (_disposed) return;

        _subscription?.Dispose();
        _subscription = null;

        if (!_listener.IsListening)
        {
            Log.Debug("HTTP server is already stopped");
            return;
        }

        _listener.Stop();
        Log.Information("Stopping HTTP server");
    }
}