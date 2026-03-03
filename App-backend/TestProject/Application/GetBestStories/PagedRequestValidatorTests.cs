using App.Application.GetBestStories;
using App.CrossCutting.RequestObjects;

namespace TestProject.Application.GetBestStories;

public class PagedRequestValidatorTests
{
    private readonly PagedRequestValidator _validator = new();

    [Fact]
    public void Validate_WhenValid_ReturnsOk()
    {
        var request = new PagedRequest { PageSize = 10, Cursor = null };
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithValidNumericCursor_ReturnsOk()
    {
        var request = new PagedRequest { PageSize = 5, Cursor = "12345" };
        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(501)]
    [InlineData(1000)]
    public void Validate_WhenPageSizeOutOfRange_ReturnsFail(int pageSize)
    {
        var request = new PagedRequest { PageSize = pageSize };
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("1", result.Errors[0]);
        Assert.Contains("500", result.Errors[0]);
    }

    [Fact]
    public void Validate_WhenCursorNotNumeric_ReturnsFail()
    {
        var request = new PagedRequest { PageSize = 10, Cursor = "not-a-number" };
        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Contains("Cursor", result.Errors[0]);
    }

    [Fact]
    public void Validate_WhenNullRequest_ReturnsFail()
    {
        var result = _validator.Validate(null!);
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
    }
}
