using MusicGrabber.Modules.Download.Application.Ports.Driven;
using MusicGrabber.Modules.Download.Application.UseCases.StartDownload;
using MusicGrabber.Modules.Download.Domain;
using MusicGrabber.Shared.Contracts;
using MusicGrabber.Shared.DTOs;

namespace MusicGrabber.Modules.Download.UnitTests.UseCases;

public class StartDownloadHandlerTests
{
    private readonly IDownloadJobRepository _repo = Substitute.For<IDownloadJobRepository>();
    private readonly IQuotaFacade _quotaFacade = Substitute.For<IQuotaFacade>();
    private readonly StartDownloadHandler _sut;

    public StartDownloadHandlerTests()
    {
        _sut = new StartDownloadHandler(_repo, _quotaFacade);
    }

    [Fact]
    public async Task StartAsync_QuotaOk_CreatesJob()
    {
        _quotaFacade.CheckAsync("user1", Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(true);
        _repo.GetActiveCountByUserIdAsync("user1", Arg.Any<CancellationToken>()).Returns(0);
        _repo.GetActiveCountAsync(Arg.Any<CancellationToken>()).Returns(0);

        var request = new StartDownloadRequest("https://youtube.com/watch?v=abc", "user1", "Mp3");
        var jobId = await _sut.StartAsync(request);

        jobId.Should().NotBeEmpty();
        await _repo.Received(1).AddAsync(Arg.Any<DownloadJob>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_QuotaExceeded_ThrowsInvalidOperation()
    {
        _quotaFacade.CheckAsync("user1", Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(false);

        var request = new StartDownloadRequest("https://youtube.com/watch?v=abc", "user1", "Mp3");

        var act = () => _sut.StartAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*quota*");
    }

    [Fact]
    public async Task StartAsync_PerUserLimitReached_ThrowsInvalidOperation()
    {
        _quotaFacade.CheckAsync("user1", Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(true);
        _repo.GetActiveCountByUserIdAsync("user1", Arg.Any<CancellationToken>()).Returns(3);
        _repo.GetActiveCountAsync(Arg.Any<CancellationToken>()).Returns(0);

        var request = new StartDownloadRequest("https://youtube.com/watch?v=abc", "user1", "Mp3");

        var act = () => _sut.StartAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*concurrent*");
    }

    [Fact]
    public async Task StartAsync_GlobalLimitReached_ThrowsInvalidOperation()
    {
        _quotaFacade.CheckAsync("user1", Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(true);
        _repo.GetActiveCountByUserIdAsync("user1", Arg.Any<CancellationToken>()).Returns(0);
        _repo.GetActiveCountAsync(Arg.Any<CancellationToken>()).Returns(9);

        var request = new StartDownloadRequest("https://youtube.com/watch?v=abc", "user1", "Mp3");

        var act = () => _sut.StartAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*global*concurrent*");
    }
}
