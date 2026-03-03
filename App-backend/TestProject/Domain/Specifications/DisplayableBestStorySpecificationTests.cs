using App.Domain.Entities;
using App.Domain.Specifications;

namespace TestProject.Domain.Specifications;

public class DisplayableBestStorySpecificationTests
{
    private readonly DisplayableBestStorySpecification _spec = new();

    [Fact]
    public void IsSatisfiedBy_WhenTitleNotEmpty_ReturnsTrue()
    {
        var story = new BestStory { Title = "A story", Uri = "", PostedBy = "", Time = "", Score = 0, CommentCount = 0 };
        Assert.True(_spec.IsSatisfiedBy(story));
    }

    [Fact]
    public void IsSatisfiedBy_WhenTitleEmpty_ReturnsFalse()
    {
        var story = new BestStory { Title = "", Uri = "", PostedBy = "", Time = "", Score = 0, CommentCount = 0 };
        Assert.False(_spec.IsSatisfiedBy(story));
    }

    [Fact]
    public void IsSatisfiedBy_WhenTitleWhitespace_ReturnsFalse()
    {
        var story = new BestStory { Title = "   ", Uri = "", PostedBy = "", Time = "", Score = 0, CommentCount = 0 };
        Assert.False(_spec.IsSatisfiedBy(story));
    }

    [Fact]
    public void IsSatisfiedBy_WhenCandidateNull_ReturnsFalse()
    {
        Assert.False(_spec.IsSatisfiedBy(null!));
    }
}
