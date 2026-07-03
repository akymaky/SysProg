using System.Text.Json.Serialization;

namespace _03_34_SysProg.Models;

public class NytApiResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("num_results")]
    public int NumResults { get; set; }

    [JsonPropertyName("results")]
    public List<NytApiResult> Results { get; set; } = new();
}