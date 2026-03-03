namespace App.CrossCutting.Validation;

/// <summary>
/// Validation result (Validator pattern).
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];

    public static ValidationResult Ok() => new() { IsValid = true };
    public static ValidationResult Fail(IEnumerable<string> errors) => new()
    {
        IsValid = false,
        Errors = errors.ToList()
    };
}
