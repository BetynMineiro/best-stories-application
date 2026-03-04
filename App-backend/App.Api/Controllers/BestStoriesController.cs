using App.Application.GetBestStories;
using App.CrossCutting.RequestObjects;
using App.CrossCutting.ResultObjects;
using App.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers;

/// <summary>
/// Returns the first best stories from Hacker News, ordered by score descending.
/// Validation in use case; errors are returned by NotificationServiceMiddleware (400 + Result messages).
/// </summary>
[ApiController]
[Route("v1/[controller]")]
[Produces("application/json")]
public class BestStoriesController(IGetBestStoriesUseCase getBestStoriesUseCase) : ControllerBase
{
    /// <summary>
    /// Gets the first best stories from Hacker News, ordered by score descending.
    /// </summary>
    /// <param name="pageSize">Number of items to return (1-500).</param>
    /// <param name="cursor">Optional cursor (last ID from previous response).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with data = CursorPage of BestStory (items, nextCursor, hasNext).</returns>
    [HttpGet]
    [ProducesResponseType(typeof(Result<CursorPage<BestStory>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result<object>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<Result<CursorPage<BestStory>>>> GetBestStories(
        [FromQuery] int pageSize = 10,
        [FromQuery] string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var request = new PagedRequest { PageSize = pageSize, Cursor = cursor };
        var page = await getBestStoriesUseCase.ExecuteAsync(request, cancellationToken);

        // Notification errors are handled by NotificationServiceMiddleware (400 + Result with messages).
        return Ok(Result<CursorPage<BestStory>>.Ok(page));
    }
}
