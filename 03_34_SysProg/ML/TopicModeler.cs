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
            "chip", "phone", "semiconductor", "startup", "cloud", "encryption", "artificial intelligence",
            "machine learning", "cybersecurity"
        ],
        ["Business"] =
        [
            "market", "stock", "economy", "trade", "company", "business", "finance", "money", "investor", "bank", "ceo",
            "revenue", "profit", "inflation", "merger", "acquisition", "earnings", "shares", "wall street",
            "interest rates"
        ],
        ["Health"] =
        [
            "health", "medical", "doctor", "hospital", "covid", "disease", "vaccine", "drug", "patient", "treatment",
            "cancer", "medicine", "clinical", "symptoms", "diagnosis", "surgery", "public health", "mental health",
            "epidemic", "outbreak", "bacteria"
        ],
        ["Science"] =
        [
            "space", "nasa", "climate", "earth", "scientist", "research", "study", "physics", "chemistry", "biology",
            "planet", "molecule", "astronomy", "telescope", "fossil", "genome", "laboratory", "experiment",
            "researchers"
        ],
        ["Sports"] =
        [
            "game", "team", "player", "league", "championship", "score", "win", "sport", "basketball", "football",
            "soccer", "tennis"
        ],
        ["Entertainment"] =
        [
            "movie", "film", "music", "actor", "celebrity", "show", "album", "concert", "hollywood", "oscar", "grammy",
            "emmy",
            "netflix", "podcast", "tv", "television", "cinema", "director", "producer", "cinematography", "cinema"
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

        var numClusters = Math.Min(_topicKeywords.Count, Math.Max(2, articles.Count / 4));

        var documents = articles.Select((a, i) => new MlDocument
        {
            Id = i,
            Text = a.FullText
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
            TopicName = clusterLabels.GetValueOrDefault((int)results[i].ClusterId, "Miscellaneous")
        }).ToList();
    }

    private string LabelCluster(List<NytArticle> articles)
    {
        var text = string.Join(" ", articles.Select(a => a.FullText))
            .ToLowerInvariant();

        var scored = _topicKeywords.Select(kvp => new
        {
            Topic = kvp.Key,
            Score = kvp.Value.Count(text.Contains)
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