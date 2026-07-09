using _03_34_SysProg.Messages;
using _03_34_SysProg.Models;
using Akka.Actor;
using Serilog;

namespace _03_34_SysProg.Actors;

public class TopicModelingActor : ReceiveActor, IWithTimers
{
    private const int MinimumArticlesForAnalysis = 5;
    private const int MinimumNewArticlesForReanalysis = 3;
    private const int MaximumArticlesPerPeriod = 100;

    private static readonly TimeSpan AnalysisDebounce = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MaximumStaleness = TimeSpan.FromSeconds(30);
    private static readonly NytPeriod[] Periods = [NytPeriod.Day, NytPeriod.Week, NytPeriod.Month];

    private readonly Dictionary<NytPeriod, TopicPeriodState> _states = new();

    public TopicModelingActor()
    {
        var worker = Context.ActorOf(TopicWorkerActor.Create(), "topicWorker");

        foreach (var period in Periods)
            _states[period] = new TopicPeriodState();

        Receive<AddArticle>(msg =>
        {
            var state = GetState(msg.Period);

            if (state.Articles.Any(a => a.Url == msg.Article.Url))
                return;

            state.Articles.Add(msg.Article);
            TrimOldArticles(state);

            state.Dirty = true;

            Log.Information(
                "[Topics] Buffered article for period={Period}, totalArticles={Count}, title={Title}",
                msg.Period,
                state.Articles.Count,
                msg.Article.Title);

            TryScheduleAnalysis(msg.Period, state);
        });

        Receive<ComputeTopics>(msg =>
        {
            var state = GetState(msg.Period);
            state.AnalysisScheduled = false;

            if (!ShouldAnalyze(state))
                return;

            if (state.AnalysisInProgress)
            {
                state.Dirty = true;
                TryScheduleAnalysis(msg.Period, state);
                return;
            }

            state.AnalysisInProgress = true;
            state.Dirty = false;

            var articlesSnapshot = state.Articles
                .OrderByDescending(a => a.PublishedDate)
                .Take(MaximumArticlesPerPeriod)
                .OrderBy(a => a.PublishedDate)
                .ToList();

            worker.Tell(new AnalyzeTopics(msg.Period, articlesSnapshot), Self);

            Log.Information(
                "[Topics] Sent topic modeling job to worker for period={Period}, articles={Count}",
                msg.Period,
                articlesSnapshot.Count);
        });

        Receive<GetTopicsByPeriod>(msg =>
        {
            var state = GetState(msg.Period);
            Sender.Tell(new TopicsSnapshot(state.LastTopics));
        });

        Receive<TopicAnalysisCompleted>(msg =>
        {
            var state = GetState(msg.Period);

            state.AnalysisInProgress = false;
            state.LastAnalyzedArticleCount = state.Articles.Count;
            state.LastAnalyzedAt = DateTime.UtcNow;
            state.LastAssignments = msg.Assignments;

            state.LastTopics = state.LastAssignments
                .GroupBy(a => a.TopicName)
                .Select(g => new TopicResult
                {
                    Topic = g.Key,
                    ArticleTitles = g.Select(x => x.Article.Title).ToList()
                })
                .ToList();

            Log.Information(
                "[Topics] Computed for period={Period}, topics={Count}, analyzedArticles={ArticleCount}",
                msg.Period,
                state.LastTopics.Count,
                state.LastAnalyzedArticleCount);

            if (state.Dirty)
                TryScheduleAnalysis(msg.Period, state);
        });

        Receive<TopicAnalysisFailed>(msg =>
        {
            var state = GetState(msg.Period);

            state.AnalysisInProgress = false;
            state.Dirty = true;

            Log.Error(
                "[Topics] Failed to compute for period={Period}. Error={Error}",
                msg.Period,
                msg.Error);

            TryScheduleAnalysis(msg.Period, state);
        });

        foreach (var period in Periods)
            Timers.StartPeriodicTimer(
                $"recompute-{period}",
                new ComputeTopics(period),
                TimeSpan.FromSeconds(60),
                TimeSpan.FromSeconds(60));
    }

    public ITimerScheduler Timers { get; set; } = null!;

    public static Props Create()
    {
        return Props.Create(() => new TopicModelingActor());
    }

    private TopicPeriodState GetState(NytPeriod period)
    {
        if (_states.TryGetValue(period, out var state))
            return state;

        state = new TopicPeriodState();
        _states[period] = state;
        return state;
    }

    private void TryScheduleAnalysis(NytPeriod period, TopicPeriodState state)
    {
        if (state.AnalysisScheduled)
            return;

        if (state.AnalysisInProgress)
            return;

        if (state.Articles.Count < MinimumArticlesForAnalysis)
            return;

        var newArticleCount = state.Articles.Count - state.LastAnalyzedArticleCount;
        var stale = DateTime.UtcNow - state.LastAnalyzedAt >= MaximumStaleness;
        var enoughNewArticles = newArticleCount >= MinimumNewArticlesForReanalysis;
        var neverAnalyzed = state.LastAnalyzedArticleCount == 0;

        if (!neverAnalyzed && !enoughNewArticles && !stale)
            return;

        state.AnalysisScheduled = true;

        Timers.StartSingleTimer(
            $"compute-topics-{period}",
            new ComputeTopics(period),
            AnalysisDebounce);

        Log.Information(
            "[Topics] Scheduled analysis for period={Period} after {Delay}. totalArticles={Total}, newArticles={NewArticles}",
            period,
            AnalysisDebounce,
            state.Articles.Count,
            newArticleCount);
    }

    private static bool ShouldAnalyze(TopicPeriodState state)
    {
        if (state.Articles.Count < MinimumArticlesForAnalysis || state.AnalysisInProgress)
            return false;

        var neverAnalyzed = state.LastAnalyzedArticleCount == 0;
        var newArticleCount = state.Articles.Count - state.LastAnalyzedArticleCount;
        var enoughNewArticles = newArticleCount >= MinimumNewArticlesForReanalysis;
        var stale = DateTime.UtcNow - state.LastAnalyzedAt >= MaximumStaleness;

        return state.Dirty && (neverAnalyzed || enoughNewArticles || stale);
    }

    private static void TrimOldArticles(TopicPeriodState state)
    {
        if (state.Articles.Count <= MaximumArticlesPerPeriod)
            return;

        state.Articles.Sort((left, right) => right.PublishedDate.CompareTo(left.PublishedDate));

        var removedCount = state.Articles.Count - MaximumArticlesPerPeriod;
        state.Articles.RemoveRange(MaximumArticlesPerPeriod, removedCount);

        state.LastAnalyzedArticleCount = Math.Min(
            state.LastAnalyzedArticleCount,
            state.Articles.Count);
    }

    private sealed class TopicPeriodState
    {
        public List<NytArticle> Articles { get; } = [];
        public List<ArticleTopic> LastAssignments { get; set; } = [];
        public List<TopicResult> LastTopics { get; set; } = [];
        public int LastAnalyzedArticleCount { get; set; }
        public bool Dirty { get; set; }
        public bool AnalysisScheduled { get; set; }
        public bool AnalysisInProgress { get; set; }
        public DateTime LastAnalyzedAt { get; set; } = DateTime.MinValue;
    }
}