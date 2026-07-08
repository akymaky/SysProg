using _03_34_SysProg.Models;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace _03_34_SysProg.ML;

public class TopicModeler
{
    private readonly MLContext _mlContext = new(42);

    private readonly Dictionary<string, string[]> _topicKeywords = new()
    {
        ["Politics"] =
        [
            "election", "vote", "president", "congress", "senate", "political", "law", "policy", "government", "trump",
            "biden", "republican", "democrat"
        ],
        ["Technology"] =
        [
            "ai", "software", "tech", "internet", "computer", "digital", "app", "data", "cyber", "robot", "algorithm",
            "chip", "phone"
        ],
        ["Business"] =
        [
            "market", "stock", "economy", "trade", "company", "business", "finance", "money", "investor", "bank", "ceo",
            "revenue", "profit"
        ],
        ["Health"] =
        [
            "health", "medical", "doctor", "hospital", "covid", "disease", "vaccine", "drug", "patient", "treatment",
            "cancer", "medicine"
        ],
        ["Science"] =
        [
            "space", "nasa", "climate", "earth", "scientist", "research", "study", "physics", "chemistry", "biology",
            "planet", "molecule"
        ],
        ["Sports"] =
        [
            "game", "team", "player", "league", "championship", "score", "win", "sport", "basketball", "football",
            "soccer", "tennis"
        ],
        ["Entertainment"] =
        [
            "movie", "film", "music", "actor", "celebrity", "show", "album", "concert", "hollywood", "oscar", "grammy",
            "netflix"
        ]
    };

    public List<ArticleTopic> Analyze(List<NytArticle> articles)
    {
        if (articles.Count < 2)
            return articles.Select(a => new ArticleTopic
            {
                Article = a,
                ClusterId = 0,
                TopicName = "Miscellaneous"
            }).ToList();

        var numClusters = Math.Min(5, Math.Max(2, articles.Count / 5));

        var documents = articles.Select((a, i) => new MlDocument
        {
            Id = i,
            Text = $"{a.Title} {a.Abstract}"
        }).ToList();

        var data = _mlContext.Data.LoadFromEnumerable(documents);

        // TF-IDF featurization (bag of words + n-grams + TF normalization)
        var textPipeline = _mlContext.Transforms.Text
            .FeaturizeText("Features", "Text");

        var featurized = textPipeline.Fit(data).Transform(data);

        // K-Means clustering on document vectors
        var kmeans = _mlContext.Clustering.Trainers.KMeans(numberOfClusters: numClusters);

        var clusterModel = kmeans.Fit(featurized);
        var predictions = clusterModel.Transform(featurized);

        var results = _mlContext.Data
            .CreateEnumerable<ClusterPrediction>(predictions, false)
            .ToList();

        // Group by cluster and derive human-readable labels
        var clusterArticles = new Dictionary<int, List<NytArticle>>();
        for (var i = 0; i < articles.Count; i++)
        {
            var cid = (int)results[i].ClusterId;
            if (!clusterArticles.ContainsKey(cid))
                clusterArticles[cid] = [];
            clusterArticles[cid].Add(articles[i]);
        }

        var clusterLabels = clusterArticles.ToDictionary(
            kv => kv.Key,
            kv => LabelCluster(kv.Value));

        return articles.Select((a, i) => new ArticleTopic
        {
            Article = a,
            ClusterId = (int)results[i].ClusterId,
            TopicName = clusterLabels[(int)results[i].ClusterId]
        }).ToList();
    }

    private string LabelCluster(List<NytArticle> articles)
    {
        var text = string.Join(" ", articles.Select(a => $"{a.Title} {a.Abstract}"))
            .ToLowerInvariant();

        var scored = _topicKeywords.Select(kvp => new
        {
            Topic = kvp.Key,
            Score = kvp.Value.Count(kw => text.Contains(kw))
        });

        var best = scored.OrderByDescending(x => x.Score).First();
        return best.Score > 0 ? best.Topic : "Miscellaneous";
    }

    private class MlDocument
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    private class ClusterPrediction
    {
        [ColumnName("PredictedLabel")] public uint ClusterId { get; set; }

        [ColumnName("Score")] public float[] Distances { get; set; } = Array.Empty<float>();
    }
}