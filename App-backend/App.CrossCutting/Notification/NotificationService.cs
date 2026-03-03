namespace App.CrossCutting.Notification;

/// <summary>
/// In-memory implementation, scoped per request.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly List<string> _errors = [];

    public void AddError(string message) => _errors.Add(message);

    public bool HasErrors => _errors.Count > 0;

    public IReadOnlyList<string> GetErrors() => _errors.AsReadOnly();

    public void Clear() => _errors.Clear();
}
