namespace App.CrossCutting.Validation;

/// <summary>
/// Contract for validators (Validator pattern).
/// </summary>
public interface IValidator<in T>
{
    ValidationResult Validate(T value);
}
