using System.Net.Http.Json;
using System.Text.Json;
using App.Domain.Contracts;
using App.Domain.Entities;
using App.Domain.Specifications;
using Microsoft.Extensions.Logging;

namespace App.Adapters.HackerNews;

public class HackerNewsBestStoryAdapter : IBestStoryService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HackerNewsBestStoryAdapter> _logger;
    private readonly ISpecification<BestStory> _displayableSpec;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public HackerNewsBestStoryAdapter(
        HttpClient httpClient,
        ILogger<HackerNewsBestStoryAdapter> logger,
        ISpecification<BestStory> displayableSpec)
    {
        _httpClient = httpClient;
        _logger = logger;
        _displayableSpec = displayableSpec;
    }

    public async Task<int[]> GetBestStoryIdsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Requesting beststories.json");
        var response = await _httpClient.GetAsync("beststories.json", cancellationToken);
        response.EnsureSuccessStatusCode();
        var ids = await response.Content.ReadFromJsonAsync<int[]>(JsonOptions, cancellationToken);
        _logger.LogDebug("Retrieved {Count} best story IDs", ids?.Length ?? 0);
        return ids ?? [];
    }

    public async Task<BestStory?> GetStoryByIdAsync(int storyId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Requesting item {Id}", storyId);
        var response = await _httpClient.GetAsync($"item/{storyId}.json", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to get item {Id}: {StatusCode}", storyId, response.StatusCode);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(json) || json == "null")
            return null;

        var raw = JsonSerializer.Deserialize<HackerNewsItem>(json, JsonOptions);
        if (raw == null)
            return null;

        var bestStory = MapToBestStory(raw);
        return _displayableSpec.IsSatisfiedBy(bestStory) ? bestStory : null;
    }

    private static BestStory MapToBestStory(HackerNewsItem item)
    {
        var timeUtc = DateTimeOffset.FromUnixTimeSeconds(item.Time);
        return new BestStory
        {
            Title = item.Title ?? "",
            Uri = item.Url ?? "",
            PostedBy = item.By ?? "",
            Time = timeUtc.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Score = item.Score,
            CommentCount = item.Descendants ?? 0
        };
    }
}
