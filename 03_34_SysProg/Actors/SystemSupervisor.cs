using _03_34_SysProg.Messages;
using Akka.Actor;

namespace _03_34_SysProg.Actors;

public class SystemSupervisor : ReceiveActor
{
    private readonly IActorRef _pipeline;
    private readonly IActorRef _query;

    public SystemSupervisor()
    {
        _pipeline = Context.ActorOf(DataPipelineActor.Create(), "pipelineActor");
        _query = Context.ActorOf(QueryActor.Create(_pipeline), "queryActor");

        Receive<AddArticle>(message => _pipeline.Forward(message));
        Receive<GetCurrentState>(message => _query.Forward(message));
    }

    protected override SupervisorStrategy SupervisorStrategy()
    {
        return new OneForOneStrategy(
            5,
            TimeSpan.FromSeconds(60),
            Decider.From(ex => ex switch
            {
                InvalidOperationException => Directive.Resume,
                IOException => Directive.Restart,
                ArgumentException => Directive.Stop,
                _ => Directive.Escalate
            })
        );
    }

    public static Props Create()
    {
        return Props.Create(() => new SystemSupervisor());
    }
}