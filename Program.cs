using System.Net;
using _01_34_SysProg;

var shutdownEvent = new ManualResetEvent(false);

int workers = Environment.ProcessorCount;
string rootPath = Path.Join(Directory.GetCurrentDirectory(), "public");
string prefix = "http://localhost:8080/";

var queue = new RequestQueue<HttpListenerContext>();
var searchService = new SearchService(rootPath);
var handler = new RequestHandler(searchService);
var workerPool = new WorkerPool(workers, queue, handler);
var server = new HttpServer(prefix, queue);

workerPool.Start();
server.Start();

Console.CancelKeyPress += (sender, eventArgs) =>
{
    eventArgs.Cancel = true;
    Console.WriteLine("Shutting down...");
    shutdownEvent.Set();
};

Console.WriteLine("Press CTRL+C to stop.");

shutdownEvent.WaitOne();

server.Stop();
queue.Stop();

Console.WriteLine("Shutdown complete.");