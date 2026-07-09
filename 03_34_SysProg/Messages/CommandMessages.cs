using _03_34_SysProg.Models;

namespace _03_34_SysProg.Messages;

public record AddArticle(NytArticle Article, NytPeriod Period);

public record ComputeTopics;

public record AnalyzeTopics(NytPeriod Period, List<NytArticle> Articles);