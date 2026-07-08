using _03_34_SysProg.Messages;
using _03_34_SysProg.ML;
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
        var modeler = new TopicModeler();

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

            if (!_isComputing[p] && _buffer[p].Count >= 5) Self.Tell(new ComputeTopics());
        });

        Receive<ComputeTopics>(_ =>
        {
            foreach (var p in _buffer.Keys.Where(p => !_isComputing[p] && _buffer[p].Count >= 5))
            {
                _isComputing[p] = true;
                
                var period = p;
                var articlesSnapshot = _buffer[period].ToList();
                
                Task.Run(() => modeler.Analyze(articlesSnapshot))
                    .PipeTo(
                        Self,
                        Self,
                        result => new TopicsComputed(result, p),
                        ex => new TopicsFailed(ex, p));
                Log.Information("[Topics] Recomputing for period={Period}", p);
            }
        });

        Receive<GetTopicsByPeriod>(msg =>
        {
            var results = _lastTopics[msg.Period];
            Sender.Tell(new TopicsSnapshot(results));
        });

        Receive<TopicsComputed>(msg =>
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
            Log.Information("[Topics] Computed for period={Period}", p);
        });

        Receive<TopicsFailed>(msg =>
        {
            Log.Error(msg.Exception, "[Topics] Failed to compute for period={Period}", msg.Period);
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

    private record TopicsComputed(List<ArticleTopic> Assignments, NytPeriod Period);

    private record TopicsFailed(Exception Exception, NytPeriod Period);
}