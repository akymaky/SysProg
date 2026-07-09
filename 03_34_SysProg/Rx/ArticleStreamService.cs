using System.Collections.Immutable;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text.Json;
using _03_34_SysProg.Messages;
using _03_34_SysProg.Models;
using Akka.Actor;
using Serilog;

namespace _03_34_SysProg.Rx;

public sealed class ArticleStreamService(string apiKey, IActorRef pipelineTarget) : IDisposable
{
    private readonly HttpClient _httpClient = new();
    private bool _disposed;
    private IDisposable? _subscription;

    public void Dispose()
    {
        if (_disposed) return;

        Stop();

        _disposed = true;
        _httpClient.Dispose();

        Log.Information("Disposing article stream service...");
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        Log.Information("Starting article stream service...");

        var periods = new[] { NytPeriod.Day, NytPeriod.Week, NytPeriod.Month };
        var streams = periods.Select(CreatePeriodStream);

        _subscription = streams.Merge().Subscribe(
            _ => { }, // side effects happen in Do()
            ex => Log.Error(ex, "Rx stream fatal error"));

        Log.Information(
            "Rx Stream: Started periodic polling for periods {Periods}",
            string.Join(", ", periods));
    }

    private IObservable<NytArticle> CreatePeriodStream(NytPeriod period)
    {
        return Observable
            .Timer(TimeSpan.Zero, TimeSpan.FromMinutes(1), Scheduler.Default)
            .Do(_ => Log.Information("[Rx] Periodic polling for {Period}", period))
            .SelectMany(_ =>
                Observable
                    .FromAsync(() => FetchPeriodAsync(period))
                    .Catch<NytApiResponse, Exception>(ex =>
                    {
                        Log.Error(ex, "[Rx] Skipping failed fetch for {Period}; next polling tick will retry", period);
                        return Observable.Empty<NytApiResponse>();
                    }))
            .SelectMany(response => response.Results)
            .Where(r => !string.IsNullOrWhiteSpace(r.Title))
            .Select(MapToArticle)
            .ObserveOn(Scheduler.Default)
            .Do(article =>
            {
                pipelineTarget.Tell(new AddArticle(article, period));
                Log.Information("[Rx] Emitted article {Title} to {Target}", article.Title, pipelineTarget);
            });
    }

    private async Task<NytApiResponse> FetchPeriodAsync(NytPeriod period)
    {
        var nytApi = $"https://api.nytimes.com/svc/mostpopular/v2/viewed/{(int)period}.json?api-key={apiKey}";

        try
        {
            var response = await _httpClient.GetAsync(nytApi);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<NytApiResponse>(content);

            if (result?.Status != "OK") throw new Exception($"[Rx] API returned invalid status: {result?.Status}");

            Log.Information("[Rx] Fetched articles for period {period}", period);

            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[Rx] Failed to fetch {Period}", period);
            throw;
        }
    }

    private static NytArticle MapToArticle(NytApiResult result)
    {
        if (!DateTime.TryParse(result.PublishedDate, out var publishedDate))
            throw new Exception($"[Rx] Invalid published date: {result.PublishedDate}");

        return new NytArticle
        {
            Title = result.Title,
            Abstract = result.Abstract,
            Url = result.Url,
            Byline = result.Byline,
            Section = result.Section,
            PublishedDate = publishedDate,
            Keywords = result.AdxKeywords
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToImmutableList()
        };
    }

    public void Stop()
    {
        _subscription?.Dispose();
        _subscription = null;
    }
}