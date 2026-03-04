using App.CrossCutting.Notification;
using App.CrossCutting.ResultObjects;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace App.Api.Middleware;

/// <summary>
/// When the action has added notifications (validation/business errors), returns 400 with Result containing messages
/// instead of the action result. Otherwise lets the result execute normally.
/// </summary>
public class NotificationServiceMiddleware : IAsyncResultFilter
{
    private readonly INotificationService _notificationService;

    public NotificationServiceMiddleware(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        context.HttpContext.Response.ContentType = "application/json";

        var options = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        };

        if (_notificationService.HasErrors)
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            var response = Result<object>.Fail(_notificationService.GetErrors());
            await context.HttpContext.Response.WriteAsync(JsonConvert.SerializeObject(response, options));
            return;
        }

        await next();
    }
}
