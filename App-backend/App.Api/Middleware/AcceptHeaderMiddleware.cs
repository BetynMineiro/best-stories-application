using Microsoft.AspNetCore.Mvc.Filters;

namespace App.Api.Middleware;

public  class AcceptHeaderMiddleware : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var request = context.HttpContext.Request;

        if (request.ContentType != null &&
            (request.ContentType.Contains("application/json") || request.ContentType.Contains("multipart/form-data")))
        {
            context.HttpContext.Request.Headers["Accept"] = "application/json, multipart/form-data";
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}