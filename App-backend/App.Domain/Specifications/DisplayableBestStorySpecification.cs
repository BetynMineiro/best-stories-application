using App.Domain.Entities;

namespace App.Domain.Specifications;

/// <summary>
/// A best story is displayable if it has a non-empty title.
/// </summary>
public class DisplayableBestStorySpecification : ISpecification<BestStory>
{
    public bool IsSatisfiedBy(BestStory candidate)
    {
        return !string.IsNullOrWhiteSpace(candidate.Title);
    }
}
