namespace App.CrossCutting.ResultObjects;

/// <summary>
/// Standard API response contract. All responses use the same shape: success, statusCode, messages (when error), data (when success).
/// </summary>
public class Result<T>
{
    public bool Success { get; set; }
    /// <summary>HTTP status code (200, 400, 503, etc.).</summary>
    public int StatusCode { get; set; }
    public List<string>? Messages { get; set; }
    public T? Data { get; set; }

    public static Result<T> Ok(T data, int statusCode = 200) => new() { Success = true, StatusCode = statusCode, Data = data };

    public static Result<T> Fail(string message, int statusCode = 400) => new() { Success = false, StatusCode = statusCode, Messages = [message] };

    public static Result<T> Fail(IEnumerable<string> messages, int statusCode = 400) => new() { Success = false, StatusCode = statusCode, Messages = messages.ToList() };
}
