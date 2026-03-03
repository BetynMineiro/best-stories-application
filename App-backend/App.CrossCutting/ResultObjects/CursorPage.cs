namespace App.CrossCutting.ResultObjects;

/// <summary>
/// Result page with cursor-based pagination.
/// </summary>
public class CursorPage<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public string? NextCursor { get; init; }
    public bool HasNext { get; init; }
}
