using _03_34_SysProg.Logger;
using _03_34_SysProg.Models;
using _03_34_SysProg.Rx;
using DotNetEnv;
using Serilog;

var httpClient = new HttpClient();
var logger = new LoggerConfiguration()
    .WriteTo.Console(theme: LoggerColorTheme.ColorTheme)
    .CreateLogger();

Env.NoClobber().Load();

var nyTimesApiKey = Environment.GetEnvironmentVariable("NYTIMES_API_KEY");

if (string.IsNullOrEmpty(nyTimesApiKey))
{
    logger.Error("NYTIMES_API_KEY environment variable is not set");
    return;
}

var articleObservable = new ArticleObservable(httpClient, nyTimesApiKey, logger);

var completion = new TaskCompletionSource();

articleObservable.GetArticleStream(NyTimesPeriod.Week)
    .Subscribe(
        article => logger.Information("[Program] Received article: {Title}", article.Title),
        error => logger.Error("[Program] Error: {Error}", error),
        completion.SetResult
    );

await completion.Task;