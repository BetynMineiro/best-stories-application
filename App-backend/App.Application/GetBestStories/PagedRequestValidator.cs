using App.CrossCutting.RequestObjects;
using App.CrossCutting.Validation;

namespace App.Application.GetBestStories;

/// <summary>
/// Validates PagedRequest for GetBestStories: pageSize 1–500, cursor optional and numeric when provided.
/// </summary>
public class PagedRequestValidator : IValidator<PagedRequest>
{
    private const int MinPageSize = 1;
    private const int MaxPageSize = 500;

    public ValidationResult Validate(PagedRequest value)
    {
        if (value is null)
            return ValidationResult.Fail(["Request is required."]);

        var errors = new List<string>();

        if (value.PageSize < MinPageSize || value.PageSize > MaxPageSize)
            errors.Add($"PageSize must be between {MinPageSize} and {MaxPageSize}.");

        if (!string.IsNullOrEmpty(value.Cursor) && !int.TryParse(value.Cursor, out _))
            errors.Add("Cursor must be a valid numeric value (e.g. last story id).");

        return errors.Count == 0 ? ValidationResult.Ok() : ValidationResult.Fail(errors);
    }
}
