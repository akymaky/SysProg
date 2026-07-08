namespace _03_34_SysProg.Models;

public class ArticleTopic
{
    public NytArticle Article { get; set; } = new();
    public int ClusterId { get; set; }
    public string TopicName { get; set; } = string.Empty;
}