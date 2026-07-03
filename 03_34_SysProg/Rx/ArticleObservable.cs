using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text.Json;
using _03_34_SysProg.Models;
using Serilog;

namespace _03_34_SysProg.Rx;

public class ArticleObservable(HttpClient client, string apiKey, ILogger logger)
{
    public IObservable<NytArticle> GetArticleStream(NytPeriod period)
    {
        logger.Information("[Rx] Getting articles for period {Period}", period);
        return Observable
            .FromAsync(() => FetchArticlesAsync(period))
            .SelectMany(apiResponse => apiResponse.Results)
            .Where(result => !string.IsNullOrEmpty(result.Title))
            .Select(MapArticle)
            .ObserveOn(Scheduler.Default)
            .Do(article => logger.Information("[Rx] Received article: {Title}", article.Title))
            .Retry(1);
    }

    private async Task<NytApiResponse> FetchArticlesAsync(NytPeriod period)
    {
        var url = $"https://api.nytimes.com/svc/mostpopular/v2/viewed/{(int)period}.json?api-key={apiKey}";
        try {
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<NytApiResponse>(content);

            if (result?.Status != "OK")
            {
                throw new Exception($"[Rx] API returned invalid status: {result?.Status}");
            }
            
            logger.Information("[Rx] Fetched articles for period {period}", period);

            return result;
        }
        catch (Exception ex) {
            logger.Error(ex, "[Rx] Error fetching articles for period {period}", period);
            throw;
        }
    }
    
    private static NytArticle MapArticle(NytApiResult result)
    {
        DateTime.TryParse(result.PublishedDate, out var publishedDate);
        
        return new NytArticle {
            Title = result.Title,
            Abstract = result.Abstract,
            Url = result.Url,
            Byline = result.Byline,
            Section = result.Section,
            PublishedDate = publishedDate,
            Keywords = result.AdxKeywords.Split(';').Select(x => x.Trim()).ToList()
        };
    }
}