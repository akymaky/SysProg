using System.Net;
using System.Text;
using _01_34_SysProg;

var shutdownEvent = new ManualResetEvent(false);

using var api = new HttpServer("http://localhost:8080/", new RequestQueue<HttpListenerContext>());
api.Start();

Console.CancelKeyPress += (sender, eventArgs) =>
{
    eventArgs.Cancel = true;
    Console.WriteLine("Shutting down...");
    shutdownEvent.Set();
};

Console.WriteLine("Press CTRL+C to stop.");

shutdownEvent.WaitOne();

api.Stop();