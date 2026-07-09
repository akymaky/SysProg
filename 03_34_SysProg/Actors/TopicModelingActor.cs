using _03_34_SysProg.Messages;
using _03_34_SysProg.Models;
using Akka.Actor;
using Serilog;

namespace _03_34_SysProg.Actors;

public class TopicModelingActor : ReceiveActor, IWithTimers
{
    private readonly Dictionary<NytPeriod, List<NytArticle>> _buffer = new();
    private readonly Dictionary<NytPeriod, bool> _isComputing = new();

    private readonly Dictionary<NytPeriod, List<ArticleTopic>> _lastAssignments = new();
    private readonly Dictionary<NytPeriod, List<TopicResult>> _lastTopics = new();

    public TopicModelingActor()
    {
        var worker = Context.ActorOf(TopicWorkerActor.Create(), "topicWorker");

        Receive<AddArticle>(msg =>
        {
            var p = msg.Period;
            if (!_buffer.TryGetValue(p, out var value))
            {
                value = [];
                _buffer[p] = value;
                _lastAssignments[p] = [];
                _lastTopics[p] = [];
                _isComputing[p] = false;
            }

            if (value.Any(a => a.Url == msg.Article.Url)) return;

            value.Add(msg.Article);

            if (!_isComputing[p] && _buffer[p].Count >= 5)
                Self.Tell(new ComputeTopics());
        });

        Receive<ComputeTopics>(_ =>
        {
            foreach (var p in _buffer.Keys.Where(p => !_isComputing[p] && _buffer[p].Count >= 5).ToList())
            {
                _isComputing[p] = true;

                var articlesSnapshot = _buffer[p].ToList();

                worker.Tell(new AnalyzeTopics(p, articlesSnapshot), Self);

                Log.Information(
                    "[Topics] Sent topic modeling job to worker for period={Period}, articles={Count}",
                    p,
                    articlesSnapshot.Count);
            }
        });

        Receive<GetTopicsByPeriod>(msg =>
        {
            var results = _lastTopics.GetValueOrDefault(msg.Period, []);
            Sender.Tell(new TopicsSnapshot(results));
        });

        Receive<TopicAnalysisCompleted>(msg =>
        {
            var p = msg.Period;
            _isComputing[p] = false;

            _lastAssignments[p] = msg.Assignments;

            _lastTopics[p] = _lastAssignments[p]
                .GroupBy(a => a.TopicName)
                .Select(g => new TopicResult
                {
                    Topic = g.Key,
                    ArticleTitles = g.Select(x => x.Article.Title).ToList()
                })
                .ToList();

            Log.Information(
                "[Topics] Computed for period={Period}, topics={Count}",
                p,
                _lastTopics[p].Count);
        });

        Receive<TopicAnalysisFailed>(msg =>
        {
            _isComputing[msg.Period] = false;

            Log.Error(
                "[Topics] Failed to compute for period={Period}. Error={Error}",
                msg.Period,
                msg.Error);
        });

        Timers.StartPeriodicTimer(
            "recompute",
            new ComputeTopics(),
            TimeSpan.FromSeconds(60),
            TimeSpan.FromSeconds(60));
    }

    public ITimerScheduler Timers { get; set; } = null!;

    public static Props Create()
    {
        return Props.Create(() => new TopicModelingActor());
    }
}