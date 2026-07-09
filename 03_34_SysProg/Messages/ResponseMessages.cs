using _03_34_SysProg.Models;

namespace _03_34_SysProg.Messages;

public record ArticlesSnapshot(List<NytArticle> Articles);

public record TopicsSnapshot(List<TopicResult> Results);

public record AnalysisResult(int TotalArticles, List<TopicResult> Topics);

public record TopicAnalysisCompleted(NytPeriod Period, List<ArticleTopic> Assignments);

public record TopicAnalysisFailed(NytPeriod Period, string Error);