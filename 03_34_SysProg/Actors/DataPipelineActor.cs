using _03_34_SysProg.Messages;
using Akka.Actor;

namespace _03_34_SysProg.Actors;

public class DataPipelineActor : ReceiveActor
{
    public DataPipelineActor()
    {
        var store = Context.ActorOf(ArticleStoreActor.Create());

        var topic = Context.ActorOf(TopicModelingActor.Create());

        Receive<AddArticle>(msg =>
        {
            store.Tell(msg);
            topic.Tell(msg);
        });

        ReceiveAsync<GetCurrentState>(async msg =>
        {
            var timeout = TimeSpan.FromSeconds(5);

            var storeTask = store.Ask<ArticlesSnapshot>(
                new GetArticlesByPeriod(msg.Period), timeout);

            var topicsTask = topic.Ask<TopicsSnapshot>(
                new GetTopicsByPeriod(msg.Period), timeout);

            await Task.WhenAll(storeTask, topicsTask);

            var articles = await storeTask;
            var topics = await topicsTask;

            Sender.Tell(new AnalysisResult(articles.Articles.Count, topics.Results));
        });
    }

    protected override SupervisorStrategy SupervisorStrategy()
    {
        return new OneForOneStrategy(
            3,
            TimeSpan.FromSeconds(30),
            Decider.From(ex => ex switch
            {
                InvalidOperationException _ => Directive.Restart,
                ArgumentException _ => Directive.Stop,
                _ => Directive.Restart
            }));
    }

    public static Props Create()
    {
        return Props.Create(() => new DataPipelineActor());
    }
}