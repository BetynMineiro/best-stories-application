namespace App.CrossCutting.Configurations;

public class HackerNewsConfig
{
    public const string SectionName = "HackerNews";
    public string BaseAddress { get; set; } = "https://hacker-news.firebaseio.com/v0/";
    public int HttpClientTimeoutSeconds { get; set; } = 10;
}
