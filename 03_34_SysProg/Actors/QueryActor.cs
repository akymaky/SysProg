using _03_34_SysProg.Messages;
using Akka.Actor;

namespace _03_34_SysProg.Actors;

public class QueryActor : ReceiveActor
{
    public QueryActor(IActorRef pipeline)
    {
        ReceiveAsync<GetCurrentState>(async msg =>
        {
            var result = await pipeline.Ask<AnalysisResult>(
                msg,
                TimeSpan.FromSeconds(10));
            Sender.Tell(result);
        });
    }

    public static Props Create(IActorRef pipeline)
    {
        return Props.Create(() => new QueryActor(pipeline));
    }
}