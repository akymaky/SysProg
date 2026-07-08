using _03_34_SysProg.Models;

namespace _03_34_SysProg.Messages;

public record ArticlesSnapshot(List<NytArticle> Articles);

public record TopicsSnapshot(List<TopicResult> Results);

public record AnalysisResult(int TotalArticles, List<TopicResult> Topics);