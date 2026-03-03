using App.Domain.Entities;

namespace App.Domain.Contracts;

/// <summary>
/// Service contract for fetching best stories (IDs and detail per item).
/// </summary>
public interface IBestStoryService
{
    Task<int[]> GetBestStoryIdsAsync(CancellationToken cancellationToken = default);
    Task<BestStory?> GetStoryByIdAsync(int storyId, CancellationToken cancellationToken = default);
}
