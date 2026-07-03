using System.Text.Json.Serialization;

namespace _03_34_SysProg.Models;

public class NyTimesApiResult
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("abstract")]
    public string Abstract { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("section")]
    public string Section { get; set; } = string.Empty;

    [JsonPropertyName("byline")]
    public string Byline { get; set; } = string.Empty;

    [JsonPropertyName("published_date")]
    public string PublishedDate { get; set; } = string.Empty;
    
    [JsonPropertyName("adx_keywords")]
    public string AdxKeywords { get; set; } = string.Empty;
}