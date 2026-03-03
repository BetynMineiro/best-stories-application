namespace App.Adapters.HackerNews;

/// <summary>
/// DTO from Hacker News API (https://hacker-news.firebaseio.com/v0/item/{id}.json).
/// </summary>
internal class HackerNewsItem
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Url { get; set; }
    public string? By { get; set; }
    public long Time { get; set; }
    public int Score { get; set; }
    public int? Descendants { get; set; }
}
