namespace App.CrossCutting.RequestObjects;

/// <summary>
/// Pagination request. Supports offset (PageNumber/PageSize) and cursor (Cursor + PageSize as limit).
/// </summary>
public class PagedRequest
{
    /// <summary>Items per page / limit (also used as limit in cursor-based pagination).</summary>
    public int PageSize { get; set; } = 10;

    /// <summary>Optional cursor for cursor-based pagination (e.g. last returned ID).</summary>
    public string? Cursor { get; set; }
}
