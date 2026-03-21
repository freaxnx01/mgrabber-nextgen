using MusicGrabber.Modules.Download.Application.Ports.Driven;
using MusicGrabber.Modules.Download.Application.UseCases.DeleteFile;
using MusicGrabber.Modules.Download.Domain;
using MusicGrabber.Shared;
using MusicGrabber.Shared.Events;

namespace MusicGrabber.Modules.Download.UnitTests.UseCases;

public class DeleteFileHandlerTests
{
    private readonly IDownloadJobRepository _repo = Substitute.For<IDownloadJobRepository>();
    private readonly IFileStorage _fileStorage = Substitute.For<IFileStorage>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly DeleteFileHandler _sut;

    public DeleteFileHandlerTests()
    {
        _sut = new DeleteFileHandler(_repo, _fileStorage, _eventBus);
    }

    [Fact]
    public async Task DeleteAsync_RemovesJobAndPublishesEvent()
    {
        var job = DownloadJob.Create("https://youtube.com/watch?v=abc", "user1", AudioFormat.Mp3);
        job.MarkCompleted("/tmp/test.mp3", 1024);
        _repo.GetByIdAsync(job.Id).Returns(job);
        _fileStorage.ExistsAsync("/tmp/test.mp3").Returns(true);

        await _sut.DeleteAsync(job.Id, "user1");

        await _fileStorage.Received(1).DeleteAsync("/tmp/test.mp3");
        await _repo.Received(1).DeleteAsync(job.Id, Arg.Any<CancellationToken>());
        await _eventBus.Received(1).PublishAsync(
            Arg.Is<FileDeletedEvent>(e => e.UserId == "user1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WrongUser_ThrowsUnauthorized()
    {
        var job = DownloadJob.Create("https://youtube.com/watch?v=abc", "user1", AudioFormat.Mp3);
        _repo.GetByIdAsync(job.Id).Returns(job);

        var act = () => _sut.DeleteAsync(job.Id, "user2");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
