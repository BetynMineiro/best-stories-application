namespace App.Domain.Entities;

/// <summary>
/// Best story response model as per Hacker News API contract.
/// </summary>
public class BestStory
{
    public string Title { get; set; } = string.Empty;
    public string Uri { get; set; } = string.Empty;
    public string PostedBy { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty; // ISO 8601
    public int Score { get; set; }
    public int CommentCount { get; set; }
}
