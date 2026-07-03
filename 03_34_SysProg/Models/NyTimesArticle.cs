namespace _03_34_SysProg.Models;

public class NyTimesArticle
{
    public string Title { get; set; } = string.Empty;
    public string Abstract { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Byline { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public DateTime PublishedDate { get; set; } = DateTime.UtcNow;
    
    public List<string> Keywords { get; set; } = new();

    public string FullText =>
        $"{Title}. {Abstract}. Section: {Section}. " +
        $"Keywords: {string.Join("; ", Keywords)}";
}