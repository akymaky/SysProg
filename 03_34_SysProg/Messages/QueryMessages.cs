using _03_34_SysProg.Models;

namespace _03_34_SysProg.Messages;

public record GetCurrentState(NytPeriod Period);

public record GetArticlesByPeriod(NytPeriod Period);

public record GetSentimentsByPeriod(NytPeriod Period);

public record GetTopicsByPeriod(NytPeriod Period);