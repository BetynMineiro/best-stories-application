using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace App.Api.Swagger;

/// <summary>
/// Adds OpenAPI descriptions for GET /v1/BestStories: cursor-based pagination and standard response contract.
/// </summary>
public class BestStoriesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.ActionDescriptor.RouteValues["controller"] != "BestStories")
            return;

        operation.Summary = "Best stories from Hacker News, ordered by score (desc). Cursor-based pagination.";
        operation.Description = "Returns a page of best stories. Use **pageSize** (1–500) and optionally **cursor** (last story id from previous response) for the next page. All responses use the same contract: `success`, `statusCode`, `messages` (on error), `data` (on success with `items`, `nextCursor`, `hasNext`).";

        foreach (var p in operation.Parameters!)
        {
            if (p.Name != "pageSize") continue;
            p.Description = "Number of items per page (1–500). Default: 10.";
            break;
        }

        foreach (var p in operation.Parameters)
        {
            if (p.Name == "cursor")
            {
                p.Description = "Optional. ID of the last story from the previous page; use `data.nextCursor` from the previous response.";
                break;
            }
        }
    }
}
