using System.Net;
using _01_34_SysProg;

var shutdownEvent = new ManualResetEvent(false);

var workers = Environment.ProcessorCount;
var rootPath = Path.Join(Directory.GetCurrentDirectory(), "public");
var prefix = "http://localhost:8080/";

var cts = new CancellationTokenSource();

var queue = new RequestQueue<HttpListenerContext>();
var searchService = new SearchService(rootPath);
var cache = new TtlCache<GifCacheItem>(TimeSpan.FromMinutes(5));
var cacheMaintenance = new CacheMaintenanceService<GifCacheItem>(cache, cts.Token, TimeSpan.FromSeconds(30));
var handler = new RequestHandler(searchService, cache);
var workerPool = new WorkerPool(workers, queue, handler);
var server = new HttpServer(prefix, queue, cts.Token);

workerPool.Start();
cacheMaintenance.Start();
server.Start();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    Logger.Info("Shutting down...");
    cts.Cancel();
    shutdownEvent.Set();
};

Logger.Info("Press CTRL+C to stop.");

shutdownEvent.WaitOne();

server.Stop();
queue.Stop();

workerPool.Join();
cacheMaintenance.Join();

Logger.Info("Shutdown complete.");