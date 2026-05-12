using System.Net;
using _01_34_SysProg;

var shutdownEvent = new ManualResetEvent(false);

int workers = Environment.ProcessorCount;
string rootPath = Path.Join(Directory.GetCurrentDirectory(), "public");
string prefix = "http://localhost:8080/";

var cts = new  CancellationTokenSource();

var queue = new RequestQueue<HttpListenerContext>();
var searchService = new SearchService(rootPath);
var cache = new TtlCache(TimeSpan.FromMinutes(5));
var cacheMaintenance = new CacheMaintenanceService(cache, cts.Token, TimeSpan.FromSeconds(30));
var handler = new RequestHandler(searchService, cache);
var workerPool = new WorkerPool(workers, queue, handler);
var server = new HttpServer(prefix, queue, cts.Token);

workerPool.Start();
cacheMaintenance.Start();
server.Start();

Console.CancelKeyPress += (sender, eventArgs) =>
{
    eventArgs.Cancel = true;
    Console.WriteLine("Shutting down...");
    cts.Cancel();
    shutdownEvent.Set();
};

Console.WriteLine("Press CTRL+C to stop.");

shutdownEvent.WaitOne();

server.Stop();
queue.Stop();

Console.WriteLine("Shutdown complete.");