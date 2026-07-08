using System.Collections.Immutable;

namespace _03_34_SysProg.Models;

public class NytArticle
{
    public string Title { get; init; } = string.Empty;
    public string Abstract { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string Byline { get; init; } = string.Empty;
    public string Section { get; init; } = string.Empty;
    public DateTime PublishedDate { get; init; } = DateTime.UtcNow;

    public ImmutableList<string> Keywords { get; init; } = [];

    public string FullText => $"{Title}. {Abstract}";
}