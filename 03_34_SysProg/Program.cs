using _03_34_SysProg.Actors;
using _03_34_SysProg.Logger;
using _03_34_SysProg.Rx;
using Akka.Actor;
using DotNetEnv;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(theme: LoggerColorTheme.ColorTheme)
    .CreateLogger();

Env.NoClobber().Load();

var nytApiKey = Environment.GetEnvironmentVariable("NYT_API_KEY");

if (string.IsNullOrEmpty(nytApiKey))
{
    Log.Error("NYT_API_KEY environment variable is not set");
    return;
}

using var system = ActorSystem.Create("NytSystem");
var supervisor = system.ActorOf(SystemSupervisor.Create());

var shutdownTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

using var streamService = new ArticleStreamService(nytApiKey, supervisor);
streamService.Start();

using var httpServer = new HttpServer("http://localhost:30000/", supervisor);
httpServer.Start();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    Log.Information("CTRL+C received. Initiating graceful shutdown...");
    shutdownTcs.TrySetResult();
};

AppDomain.CurrentDomain.ProcessExit += (_, _) =>
{
    Log.Information("SIGTERM received. Initiating graceful shutdown...");
    shutdownTcs.TrySetResult();
};

await shutdownTcs.Task;
await system.Terminate();
await Log.CloseAndFlushAsync();