using App.CrossCutting.Configurations;
using App.CrossCutting.Notification;
using App.CrossCutting.RequestObjects;
using App.CrossCutting.ResultObjects;
using App.CrossCutting.Validation;
using App.Domain.Contracts;
using App.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace App.Application.GetBestStories;

public class GetBestStoriesUseCase(
    IBestStoryService bestStoryService,
    ICacheService cacheService,
    IValidator<PagedRequest> pagedRequestValidator,
    INotificationService notification,
    ILogger<GetBestStoriesUseCase> logger,
    IOptions<CacheConfig> cacheConfigOptions) : IGetBestStoriesUseCase
{
    private const string BestStoryIdsCacheKey = "beststories_ids";
    private const string StoryCacheKeyPrefix = "story_";
    private const int MaxPageSize = 500;
    private readonly CacheConfig _cacheConfig = cacheConfigOptions.Value;

    public async Task<CursorPage<BestStory>> ExecuteAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var validation = pagedRequestValidator.Validate(request);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
                notification.AddError(error);
            return new CursorPage<BestStory>();
        }

        var limit = request.PageSize;
        if (limit <= 0) limit = 1;
        if (limit > MaxPageSize) limit = MaxPageSize;

        var bestStoryIds = await cacheService.GetOrCreateAsync(BestStoryIdsCacheKey, async () => (int[]?)await FetchBestStoryIdsAsync(),
            TimeSpan.FromSeconds((double)_cacheConfig.BestStoryIdsTtlSeconds), cancellationToken);

        if (bestStoryIds == null || bestStoryIds.Length == 0)
            return new CursorPage<BestStory>();

        var startIndex = 0;
        if (!string.IsNullOrEmpty(request.Cursor) && int.TryParse(request.Cursor, out var cursorValue))
        {
            var cursorIndex = Array.IndexOf(bestStoryIds, cursorValue);
            startIndex = cursorIndex < 0 ? 0 : cursorIndex + 1;
        }

        var storyIdsToFetch = bestStoryIds.Skip(startIndex).Take(limit).ToArray();
        if (storyIdsToFetch.Length == 0)
            return new CursorPage<BestStory>();

        var fetchTasks = storyIdsToFetch.Select(storyId => GetOrFetchStoryAsync(storyId, cancellationToken));
        var rawStories = await Task.WhenAll(fetchTasks);
        var stories = rawStories
            .Where(story => story != null)
            .Select(story => story!)
            .OrderByDescending(story => story.Score)
            .ToList();

        var lastStoryId = storyIdsToFetch[^1];
        var lastIndexInFullList = Array.IndexOf(bestStoryIds, lastStoryId);
        var hasNext = lastIndexInFullList >= 0 && lastIndexInFullList < bestStoryIds.Length - 1;
        var nextCursor = hasNext ? lastStoryId.ToString() : null;

        return new CursorPage<BestStory>
        {
            Items = stories,
            NextCursor = nextCursor,
            HasNext = hasNext
        };
    }

    private async Task<int[]> FetchBestStoryIdsAsync()
    {
        logger.LogDebug("Fetching best story IDs from best story service");
        return await bestStoryService.GetBestStoryIdsAsync();
    }

    private async Task<BestStory?> GetOrFetchStoryAsync(int storyId, CancellationToken cancellationToken)
    {
        var cacheKey = StoryCacheKeyPrefix + storyId;
        return await cacheService.GetOrCreateAsync(cacheKey, () => bestStoryService.GetStoryByIdAsync(storyId, cancellationToken),
            TimeSpan.FromSeconds((double)_cacheConfig.StoryDetailTtlSeconds), cancellationToken);
    }
}
