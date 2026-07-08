using _03_34_SysProg.Messages;
using _03_34_SysProg.Models;
using Akka.Actor;
using Serilog;

namespace _03_34_SysProg.Actors;

public class ArticleStoreActor : ReceiveActor
{
    private readonly Dictionary<NytPeriod, HashSet<NytArticle>> _articles = new();

    public ArticleStoreActor()
    {
        Receive<AddArticle>(msg =>
        {
            if (!_articles.ContainsKey(msg.Period))
                _articles[msg.Period] = new HashSet<NytArticle>(new ArticleUrlComparer());

            if (_articles[msg.Period].Add(msg.Article))
                Log.Information(
                    "[ArticleStoreActor] Store: Added article '{Title}' for period={Period} (total={Count})",
                    msg.Article.Title, msg.Period, _articles[msg.Period].Count);
        });

        Receive<GetArticlesByPeriod>(msg =>
        {
            var list = _articles
                .GetValueOrDefault(msg.Period, [])
                .ToList();
            Sender.Tell(new ArticlesSnapshot(list));
        });
    }

    public static Props Create()
    {
        return Props.Create(() => new ArticleStoreActor());
    }

    private class ArticleUrlComparer : IEqualityComparer<NytArticle>
    {
        public bool Equals(NytArticle? x, NytArticle? y)
        {
            return x?.Url == y?.Url;
        }

        public int GetHashCode(NytArticle obj)
        {
            return obj.Url.GetHashCode();
        }
    }
}