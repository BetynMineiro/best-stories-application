using App.Application.GetBestStories;
using App.CrossCutting.Configurations;
using App.CrossCutting.Notification;
using App.CrossCutting.RequestObjects;
using App.CrossCutting.Validation;
using App.Domain.Contracts;
using App.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace TestProject.Application.GetBestStories;

public class GetBestStoriesUseCaseTests
{
    private readonly Mock<IBestStoryService> _bestStoryServiceMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IValidator<PagedRequest>> _validatorMock;
    private readonly NotificationService _notification;
    private readonly GetBestStoriesUseCase _useCase;

    public GetBestStoriesUseCaseTests()
    {
        _bestStoryServiceMock = new Mock<IBestStoryService>();
        _cacheServiceMock = new Mock<ICacheService>();
        _validatorMock = new Mock<IValidator<PagedRequest>>();
        _validatorMock.Setup(x => x.Validate(It.IsAny<PagedRequest>())).Returns(ValidationResult.Ok());
        _notification = new NotificationService();
        var loggerMock = new Mock<ILogger<GetBestStoriesUseCase>>();
        var cacheConfigOptions = Options.Create(new CacheConfig { BestStoryIdsTtlSeconds = 300, StoryDetailTtlSeconds = 180 });
        _useCase = new GetBestStoriesUseCase(
            _bestStoryServiceMock.Object,
            _cacheServiceMock.Object,
            _validatorMock.Object,
            _notification,
            loggerMock.Object,
            cacheConfigOptions);
    }

    private static BestStory Story(string title, int score) => new()
    {
        Title = title,
        Uri = "https://x.com",
        PostedBy = "u",
        Time = "1970-01-01T00:00:00Z",
        Score = score,
        CommentCount = 0
    };

    [Fact]
    public async Task ExecuteAsync_WithPagedRequest_ReturnsCursorPageSortedByScoreDescending()
    {
        var ids = new[] { 1, 2, 3 };
        _cacheServiceMock
            .Setup(x => x.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<Func<Task<int[]?>>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Returns<string, Func<Task<int[]?>>, TimeSpan?, CancellationToken>(async (_, factory, _, _) => await factory());

        _bestStoryServiceMock.Setup(x => x.GetBestStoryIdsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(ids);

        _cacheServiceMock
            .Setup(x => x.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<Func<Task<BestStory?>>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Returns<string, Func<Task<BestStory?>>, TimeSpan?, CancellationToken>(async (_, factory, _, _) => await factory());

        _bestStoryServiceMock.Setup(x => x.GetStoryByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(Story("A", 10));
        _bestStoryServiceMock.Setup(x => x.GetStoryByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(Story("B", 50));
        _bestStoryServiceMock.Setup(x => x.GetStoryByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(Story("C", 20));

        var request = new PagedRequest { PageSize = 3 };
        var page = await _useCase.ExecuteAsync(request, CancellationToken.None);

        Assert.Equal(3, page.Items.Count);
        Assert.Equal(50, page.Items[0].Score);
        Assert.Equal(20, page.Items[1].Score);
        Assert.Equal(10, page.Items[2].Score);
        Assert.Equal("B", page.Items[0].Title);
        Assert.Equal("C", page.Items[1].Title);
        Assert.Equal("A", page.Items[2].Title);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPageSizeOver500_ClampsTo500()
    {
        _cacheServiceMock
            .Setup(x => x.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<Func<Task<int[]?>>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Range(1, 600).ToArray());

        _cacheServiceMock
            .Setup(x => x.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<Func<Task<BestStory?>>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Returns<string, Func<Task<BestStory?>>, TimeSpan?, CancellationToken>(async (_, factory, _, _) => await factory());

        _bestStoryServiceMock
            .Setup(x => x.GetStoryByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => Story("T", id));

        var request = new PagedRequest { PageSize = 1000 };
        var page = await _useCase.ExecuteAsync(request, CancellationToken.None);

        Assert.Equal(500, page.Items.Count);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoIds_ReturnsEmptyCursorPage()
    {
        _cacheServiceMock
            .Setup(x => x.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<Func<Task<int[]?>>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var request = new PagedRequest { PageSize = 10 };
        var page = await _useCase.ExecuteAsync(request, CancellationToken.None);

        Assert.Empty(page.Items);
        Assert.False(page.HasNext);
        Assert.Null(page.NextCursor);
    }

    [Fact]
    public async Task ExecuteAsync_WithCursor_RespectsCursorPosition()
    {
        // ids after cursor "20" with PageSize 2 → [30, 40]; 50 remains so HasNext is true
        var ids = new[] { 10, 20, 30, 40, 50 };
        _cacheServiceMock
            .Setup(x => x.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<Func<Task<int[]?>>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Returns<string, Func<Task<int[]?>>, TimeSpan?, CancellationToken>(async (_, factory, _, _) => await factory());
        _bestStoryServiceMock.Setup(x => x.GetBestStoryIdsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(ids);

        _cacheServiceMock
            .Setup(x => x.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<Func<Task<BestStory?>>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .Returns<string, Func<Task<BestStory?>>, TimeSpan?, CancellationToken>(async (_, factory, _, _) => await factory());

        _bestStoryServiceMock.Setup(x => x.GetStoryByIdAsync(30, It.IsAny<CancellationToken>())).ReturnsAsync(Story("S30", 30));
        _bestStoryServiceMock.Setup(x => x.GetStoryByIdAsync(40, It.IsAny<CancellationToken>())).ReturnsAsync(Story("S40", 40));

        var request = new PagedRequest { PageSize = 2, Cursor = "20" };
        var page = await _useCase.ExecuteAsync(request, CancellationToken.None);

        Assert.Equal(2, page.Items.Count);
        Assert.True(page.HasNext);
        Assert.Equal("40", page.NextCursor);
    }

    [Fact]
    public async Task ExecuteAsync_WhenValidationFails_ReturnsEmptyPageAndNotifiesErrors()
    {
        _validatorMock
            .Setup(x => x.Validate(It.IsAny<PagedRequest>()))
            .Returns(ValidationResult.Fail(["PageSize must be between 1 and 500."]));

        var request = new PagedRequest { PageSize = 0 };
        var page = await _useCase.ExecuteAsync(request, CancellationToken.None);

        Assert.Empty(page.Items);
        Assert.True(_notification.HasErrors);
        Assert.Single(_notification.GetErrors(), "PageSize must be between 1 and 500.");

        // Garante que serviço e cache não são chamados quando a validação falha
        _bestStoryServiceMock.Verify(x => x.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()), Times.Never);
        _cacheServiceMock.Verify(x => x.GetOrCreateAsync(It.IsAny<string>(), It.IsAny<Func<Task<int[]?>>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
