using System.Text;
using App.Api.Middleware;
using App.CrossCutting.Notification;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json.Linq;

namespace TestProject.Api.Middleware;

public class NotificationServiceMiddlewareTests
{
    private static (ResultExecutingContext Context, ActionContext ActionContext, MemoryStream ResponseStream) CreateContext(INotificationService _)
    {
        var responseStream = new MemoryStream();
        var httpContext = new DefaultHttpContext
        {
            Response =
            {
                Body = responseStream
            }
        };

        var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new ActionDescriptor());
        var filters = new List<IFilterMetadata>();
        var result = new ObjectResult(null);
        var context = new ResultExecutingContext(actionContext, filters, result, null!);

        return (context, actionContext, responseStream);
    }

    [Fact]
    public async Task OnResultExecutionAsync_WhenHasErrors_Returns400AndResultWithMessages()
    {
        var notification = new NotificationService();
        notification.AddError("PageSize must be between 1 and 500.");
        notification.AddError("Invalid cursor.");

        var middleware = new NotificationServiceMiddleware(notification);
        var (context, actionContext, responseStream) = CreateContext(notification);

        var nextCalled = false;
        ResultExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult(new ResultExecutedContext(actionContext, context.Filters, context.Result, context.Controller));
        };

        await middleware.OnResultExecutionAsync(context, next);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.HttpContext.Response.StatusCode);
        Assert.Equal("application/json", context.HttpContext.Response.ContentType);

        responseStream.Position = 0;
        var body = Encoding.UTF8.GetString(responseStream.ToArray());
        var json = JObject.Parse(body);

        Assert.False(json["success"]?.Value<bool>());
        Assert.Equal(400, json["statusCode"]?.Value<int>());
        var messages = json["messages"]?.ToObject<List<string>>();
        Assert.NotNull(messages);
        Assert.Equal(2, messages.Count);
        Assert.Contains("PageSize must be between 1 and 500.", messages);
        Assert.Contains("Invalid cursor.", messages);
        Assert.True(json["data"] == null || json["data"]!.Type == JTokenType.Null || json["data"]!.Type == JTokenType.Undefined);
    }

    [Fact]
    public async Task OnResultExecutionAsync_WhenNoErrors_CallsNext()
    {
        var notification = new NotificationService();

        var middleware = new NotificationServiceMiddleware(notification);
        var (context, actionContext, responseStream) = CreateContext(notification);

        var nextCalled = false;
        ResultExecutionDelegate next = () =>
        {
            nextCalled = true;
            return Task.FromResult(new ResultExecutedContext(actionContext, context.Filters, context.Result, context.Controller));
        };

        await middleware.OnResultExecutionAsync(context, next);

        Assert.True(nextCalled);
        Assert.Equal(0, responseStream.Length);
    }

    [Fact]
    public async Task OnResultExecutionAsync_WhenHasErrors_ResponseMatchesResultContract()
    {
        var notification = new NotificationService();
        notification.AddError("Single error.");

        var middleware = new NotificationServiceMiddleware(notification);
        var (context, actionContext, responseStream) = CreateContext(notification);

        await middleware.OnResultExecutionAsync(context, () => Task.FromResult(new ResultExecutedContext(actionContext, context.Filters, context.Result, context.Controller)));

        responseStream.Position = 0;
        var body = Encoding.UTF8.GetString(responseStream.ToArray());
        var json = JObject.Parse(body);

        Assert.True(json.ContainsKey("success"));
        Assert.True(json.ContainsKey("statusCode"));
        Assert.True(json.ContainsKey("messages"));
    }
}
