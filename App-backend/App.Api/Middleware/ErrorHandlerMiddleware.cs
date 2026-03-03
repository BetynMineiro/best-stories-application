using System.Text.Json;
using App.CrossCutting.ResultObjects;

namespace App.Api.Middleware;

public class ErrorHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlerMiddleware> _logger;

    public ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            _logger.LogError("Something went wrong: {Message}", ex.Message);
            await HandleExceptionAsync(httpContext, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        context.Response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            HttpRequestException => (503, "External API is temporarily unavailable."),
            ArgumentException => (400, exception.Message),
            _ => (500, "Internal Server Error.")
        };

        context.Response.StatusCode = statusCode;
        var result = Result<object>.Fail(message, statusCode);
        return context.Response.WriteAsync(JsonSerializer.Serialize(result, options));
    }
}
