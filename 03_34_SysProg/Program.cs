using _03_34_SysProg.Actors;
using _03_34_SysProg.Logger;
using _03_34_SysProg.Rx;
using Akka.Actor;
using Akka.Configuration;
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

var config = ConfigurationFactory.ParseString(@"
    akka {
        actor {
            default-dispatcher {
                executor = fork-join-executor
                fork-join-executor {
                    parallelism-min = 4
                    parallelism-factor = 2.0
                    parallelism-max = 16
                }
                throughput = 100
            }

            topic-modeling-dispatcher {
                type = Dispatcher
                executor = thread-pool-executor
                thread-pool-executor {
                    fixed-pool-size = 3
                }
                throughput = 1
            }
        }
    }
");

using var system = ActorSystem.Create("NytSystem", config);
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

httpServer.Stop();
streamService.Stop();

await system.Terminate();
await Log.CloseAndFlushAsync();