using App.CrossCutting.ResultObjects;
using App.Domain.Entities;

namespace TestProject.CrossCutting.ResultObjects;

public class ResultAndCursorPageTests
{
    [Fact]
    public void Result_Ok_SetsSuccessDataAndStatusCode()
    {
        var data = new CursorPage<BestStory> { Items = [], NextCursor = null, HasNext = false };
        var result = Result<CursorPage<BestStory>>.Ok(data);

        Assert.True(result.Success);
        Assert.Equal(200, result.StatusCode);
        Assert.NotNull(result.Data);
        Assert.Same(data, result.Data);
        Assert.Null(result.Messages);
    }

    [Fact]
    public void Result_Fail_SingleMessage_SetsSuccessFalseMessagesAndStatusCode()
    {
        var result = Result<object>.Fail("Something failed", 400);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.NotNull(result.Messages);
        Assert.Single(result.Messages, "Something failed");
        Assert.Null(result.Data);
    }

    [Fact]
    public void Result_Fail_MultipleMessages_SetsAllMessagesAndStatusCode()
    {
        var result = Result<object>.Fail(["Error one", "Error two"], 400);

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(2, result.Messages!.Count);
        Assert.Contains("Error one", result.Messages);
        Assert.Contains("Error two", result.Messages);
    }

    [Fact]
    public void CursorPage_Empty_HasNoItemsAndNoNext()
    {
        var page = new CursorPage<BestStory>();

        Assert.NotNull(page.Items);
        Assert.Empty(page.Items);
        Assert.Null(page.NextCursor);
        Assert.False(page.HasNext);
    }
}
