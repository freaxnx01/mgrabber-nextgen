using MusicGrabber.Modules.Download.Application.Ports.Driven;
using MusicGrabber.Modules.Download.Application.UseCases.StartPlaylistDownload;
using MusicGrabber.Modules.Download.Domain;
using MusicGrabber.Shared.Contracts;

namespace MusicGrabber.Modules.Download.UnitTests.UseCases;

public class StartPlaylistDownloadHandlerTests
{
    private readonly IDownloadJobRepository _repo = Substitute.For<IDownloadJobRepository>();
    private readonly IQuotaFacade _quotaFacade = Substitute.For<IQuotaFacade>();
    private readonly StartPlaylistDownloadHandler _sut;

    public StartPlaylistDownloadHandlerTests()
    {
        _sut = new StartPlaylistDownloadHandler(_repo, _quotaFacade);
    }

    [Fact]
    public async Task StartPlaylistAsync_QuotaOk_CreatesBatchJobs()
    {
        _quotaFacade.CheckAsync("user1", Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(true);
        var videoUrls = new List<string>
        {
            "https://youtube.com/watch?v=a",
            "https://youtube.com/watch?v=b",
            "https://youtube.com/watch?v=c"
        };

        var jobIds = await _sut.StartPlaylistAsync("PL123", "user1", videoUrls, "Mp3", false);

        jobIds.Should().HaveCount(3);
        jobIds.Should().OnlyContain(id => id != Guid.Empty);
        await _repo.Received(3).AddAsync(Arg.Any<DownloadJob>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartPlaylistAsync_QuotaExceeded_Throws()
    {
        _quotaFacade.CheckAsync("user1", Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(false);

        var act = () => _sut.StartPlaylistAsync("PL123", "user1",
            new List<string> { "https://youtube.com/watch?v=a" }, "Mp3", false);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*quota*");
    }

    [Fact]
    public async Task StartPlaylistAsync_ParsesFlacFormat()
    {
        _quotaFacade.CheckAsync("user1", Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(true);

        var jobIds = await _sut.StartPlaylistAsync("PL123", "user1",
            new List<string> { "https://youtube.com/watch?v=a" }, "Flac", true);

        jobIds.Should().HaveCount(1);
        await _repo.Received(1).AddAsync(
            Arg.Is<DownloadJob>(j => j.Format == AudioFormat.Flac && j.NormalizeAudio),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartPlaylistAsync_SetsPlaylistId()
    {
        _quotaFacade.CheckAsync("user1", Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(true);

        await _sut.StartPlaylistAsync("PL123", "user1",
            new List<string> { "https://youtube.com/watch?v=a" }, "Mp3", false);

        await _repo.Received(1).AddAsync(
            Arg.Is<DownloadJob>(j => j.PlaylistId == "PL123"),
            Arg.Any<CancellationToken>());
    }
}
