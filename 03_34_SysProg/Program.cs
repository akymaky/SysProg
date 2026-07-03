using _03_34_SysProg.Logger;
using _03_34_SysProg.Models;
using _03_34_SysProg.Rx;
using DotNetEnv;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(theme: LoggerColorTheme.ColorTheme)
    .CreateLogger();

var httpClient = new HttpClient();
var logger = Log.ForContext<Program>();

Env.NoClobber().Load();

var nytApiKey = Environment.GetEnvironmentVariable("NYT_API_KEY");

if (string.IsNullOrEmpty(nytApiKey))
{
    logger.Error("NYT_API_KEY environment variable is not set");
    return;
}

var articleObservable = new ArticleObservable(httpClient, nytApiKey);

var completion = new TaskCompletionSource();

articleObservable.GetArticleStream(NytPeriod.Week)
    .Subscribe(
        article => logger.Information("[Program] Received article: {Title}", article.Title),
        error => logger.Error("[Program] Error: {Error}", error),
        completion.SetResult
    );

await completion.Task;