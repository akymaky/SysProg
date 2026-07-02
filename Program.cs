using System.Net;
using _02_34_SysProg;

var shutdownTcs = new TaskCompletionSource();

var maxConcurrency = Environment.ProcessorCount * 2;
var rootPath = Path.Join(Directory.GetCurrentDirectory(), "public");
var prefix = "http://localhost:8080/";

var cts = new CancellationTokenSource();

var queue = new RequestQueue<HttpListenerContext>();
var searchService = new SearchService(rootPath);
var cache = new TtlCache<GifCacheItem>(TimeSpan.FromMinutes(5));
var cacheMaintenance = new CacheMaintenanceService<GifCacheItem>(cache, cts.Token, TimeSpan.FromSeconds(30));
var handler = new RequestHandler(searchService, cache);
var workerPool = new WorkerPool(maxConcurrency, queue, handler, cts.Token);
var server = new HttpServer(prefix, queue, cts.Token);

var workerTask = workerPool.StartAsync();
var maintenanceTask = cacheMaintenance.StartAsync();
var serverTask = server.StartAsync();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    Logger.Info("Shutting down...");
    cts.Cancel();
    shutdownTcs.TrySetResult();
};

Logger.Info("Press CTRL+C to stop.");

await shutdownTcs.Task;

server.Stop();
queue.Stop();

try
{
    await Task.WhenAll(serverTask, workerTask, maintenanceTask)
        .ContinueWith(_ => { Logger.Info("All background tasks completed successfully."); },
            TaskContinuationOptions.OnlyOnRanToCompletion);
}
catch (OperationCanceledException)
{
    Logger.Info("Shutdown completed with cancellation.");
}

Logger.Info("Shutdown complete.");