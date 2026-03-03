namespace App.CrossCutting.Configurations;

public class CacheConfig
{
    public const string SectionName = "Cache";
    public int BestStoryIdsTtlSeconds { get; set; } = 300; // 5 min
    public int StoryDetailTtlSeconds { get; set; } = 180;  // 3 min
}
