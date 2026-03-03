namespace App.Domain.Specifications;

/// <summary>
/// Contract for reusable business rules (Specification pattern).
/// </summary>
public interface ISpecification<in T>
{
    bool IsSatisfiedBy(T candidate);
}
