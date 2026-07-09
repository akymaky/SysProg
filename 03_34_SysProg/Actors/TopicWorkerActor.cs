using _03_34_SysProg.Messages;
using _03_34_SysProg.ML;
using Akka.Actor;
using Akka.Routing;
using Serilog;

namespace _03_34_SysProg.Actors;

public class TopicWorkerActor : ReceiveActor
{
    private readonly TopicModeler _modeler = new();

    public TopicWorkerActor()
    {
        Receive<AnalyzeTopics>(msg =>
        {
            try
            {
                Log.Information(
                    "[TopicWorker] Starting ML.NET topic analysis for period={Period}, articles={Count}",
                    msg.Period,
                    msg.Articles.Count);

                var assignments = _modeler.Analyze(msg.Articles);

                Sender.Tell(new TopicAnalysisCompleted(msg.Period, assignments));

                Log.Information(
                    "[TopicWorker] Completed ML.NET topic analysis for period={Period}",
                    msg.Period);
            }
            catch (Exception ex)
            {
                Log.Error(
                    ex,
                    "[TopicWorker] Failed ML.NET topic analysis for period={Period}",
                    msg.Period);

                Sender.Tell(new TopicAnalysisFailed(msg.Period, ex.Message));
            }
        });
    }

    public static Props Create()
    {
        return Props.Create(() => new TopicWorkerActor())
            .WithDispatcher("akka.actor.topic-modeling-dispatcher")
            .WithRouter(new RoundRobinPool(3));
    }
}