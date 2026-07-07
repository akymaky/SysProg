using _03_34_SysProg.Messages;
using Akka.Actor;

namespace _03_34_SysProg.Actors;

public class DataPipelineActor : ReceiveActor
{
    public DataPipelineActor()
    {
        var store = Context.ActorOf(ArticleStoreActor.Create());

        Receive<AddArticle>(msg => store.Forward(msg));

        ReceiveAsync<GetCurrentState>(async msg =>
        {
            var timeout = TimeSpan.FromSeconds(5);

            var articles = await store.Ask<ArticlesSnapshot>(new GetArticlesByPeriod { Period = msg.Period }, timeout);

            Sender.Tell(new AnalysisResult(articles.Articles.Count));
        });
    }

    public static Props Create()
    {
        return Props.Create(() => new DataPipelineActor());
    }
}