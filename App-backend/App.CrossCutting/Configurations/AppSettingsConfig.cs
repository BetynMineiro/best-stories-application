namespace App.CrossCutting.Configurations;

public class AppSettingsConfig
{
    public HackerNewsConfig HackerNews { get; set; } = new();
    public CacheConfig Cache { get; set; } = new();
}
