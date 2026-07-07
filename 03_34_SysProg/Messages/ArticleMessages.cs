using _03_34_SysProg.Models;

namespace _03_34_SysProg.Messages;

public class AddArticle
{
    public NytArticle Article { get; init; } = null!;
    public NytPeriod Period { get; init; }
}

public class GetArticlesByPeriod
{
    public NytPeriod Period { get; init; }
}

public class ArticlesSnapshot
{
    public List<NytArticle> Articles { get; init; } = null!;
}