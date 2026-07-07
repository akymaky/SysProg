using System.Text.Json.Serialization;

namespace _03_34_SysProg.Models;

public class NytApiResult
{
    [JsonPropertyName("title")] public string Title { get; init; } = string.Empty;

    [JsonPropertyName("abstract")] public string Abstract { get; init; } = string.Empty;

    [JsonPropertyName("url")] public string Url { get; init; } = string.Empty;

    [JsonPropertyName("section")] public string Section { get; init; } = string.Empty;

    [JsonPropertyName("byline")] public string Byline { get; init; } = string.Empty;

    [JsonPropertyName("published_date")] public string PublishedDate { get; init; } = string.Empty;

    [JsonPropertyName("adx_keywords")] public string AdxKeywords { get; init; } = string.Empty;
}