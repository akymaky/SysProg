namespace _03_34_SysProg.Models;

public class AnalysisResponse
{
    public List<TopicResult> Topics { get; init; } = [];
    public int TotalArticles { get; init; }
    public string Period { get; init; } = string.Empty;
    public DateTime ProcessedAt { get; init; }
}