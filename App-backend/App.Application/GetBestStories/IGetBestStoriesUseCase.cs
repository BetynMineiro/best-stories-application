using App.CrossCutting.RequestObjects;
using App.CrossCutting.ResultObjects;
using App.Domain.Entities;

namespace App.Application.GetBestStories;

public interface IGetBestStoriesUseCase
{
    Task<CursorPage<BestStory>> ExecuteAsync(PagedRequest request, CancellationToken cancellationToken = default);
}
