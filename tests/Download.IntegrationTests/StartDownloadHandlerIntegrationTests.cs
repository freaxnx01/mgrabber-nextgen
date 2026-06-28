using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MusicGrabber.Modules.Download.Application.UseCases.StartDownload;
using MusicGrabber.Modules.Download.Domain;
using MusicGrabber.Modules.Download.Infrastructure.Adapters.Persistence;
using MusicGrabber.Shared.Contracts;
using MusicGrabber.Shared.DTOs;
using NSubstitute;

namespace MusicGrabber.Modules.Download.IntegrationTests;

/// <summary>
/// Exercises <see cref="StartDownloadHandler"/> against a real
/// <see cref="DownloadDbContext"/> backed by an in-memory SQLite database with
/// the module's EF Core migrations applied. This catches persistence-layer
/// regressions (e.g. missing tables, broken entity mappings) that pure unit
/// tests with mocked repositories cannot see.
/// </summary>
public sealed class StartDownloadHandlerIntegrationTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private DownloadDbContext _db = null!;
    private DownloadJobRepository _repo = null!;
    private IQuotaFacade _quotaFacade = null!;
    private StartDownloadHandler _sut = null!;

    public async Task InitializeAsync()
    {
        // Shared in-memory SQLite database: the database only exists while the
        // connection is open, so we keep a single connection alive for the
        // lifetime of the test.
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<DownloadDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new DownloadDbContext(options);

        // Apply the real migrations — this is the critical step. If the
        // module's migrations are broken or missing, this call will throw and
        // the test will fail loudly instead of giving a confusing
        // "no such table" error at query time.
        await _db.Database.MigrateAsync();

        _repo = new DownloadJobRepository(_db);
        _quotaFacade = Substitute.For<IQuotaFacade>();
        _sut = new StartDownloadHandler(_repo, _quotaFacade);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task StartAsync_HappyPath_PersistsDownloadJob()
    {
        _quotaFacade.CheckAsync("user1", Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var request = new StartDownloadRequest(
            Url: "https://youtube.com/watch?v=dQw4w9WgXcQ",
            UserId: "user1",
            Format: "Mp3",
            Title: "Never Gonna Give You Up",
            Author: "Rick Astley",
            NormalizeAudio: true);

        var jobId = await _sut.StartAsync(request);

        // The row must actually land in the database — not just in a mock.
        var persisted = await _db.DownloadJobs.AsNoTracking()
            .SingleOrDefaultAsync(j => j.Id == jobId);

        persisted.Should().NotBeNull();
        persisted!.UserId.Should().Be("user1");
        persisted.Url.Should().Be("https://youtube.com/watch?v=dQw4w9WgXcQ");
        persisted.Title.Should().Be("Never Gonna Give You Up");
        persisted.Author.Should().Be("Rick Astley");
        persisted.Format.Should().Be(AudioFormat.Mp3);
        persisted.Status.Should().Be(DownloadStatus.Pending);
        persisted.NormalizeAudio.Should().BeTrue();
        persisted.Progress.Should().Be(0);
        persisted.RetryCount.Should().Be(0);
    }

    [Fact]
    public async Task StartAsync_CountsActiveJobsFromDatabase_EnforcesPerUserLimit()
    {
        _quotaFacade.CheckAsync("user1", Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Seed three already-active jobs for the same user. The handler must
        // read these via the repository against the real DbContext and reject
        // a fourth concurrent download. This verifies both the LINQ query in
        // GetActiveCountByUserIdAsync and the handler's limit check.
        for (var i = 0; i < 3; i++)
        {
            var job = DownloadJob.Create(
                url: $"https://youtube.com/watch?v=existing{i}",
                userId: "user1",
                format: AudioFormat.Mp3);
            job.MarkDownloading();
            _db.DownloadJobs.Add(job);
        }
        await _db.SaveChangesAsync();

        var request = new StartDownloadRequest(
            "https://youtube.com/watch?v=new",
            "user1",
            "Mp3");

        var act = () => _sut.StartAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*concurrent*");

        // Nothing extra should have been inserted.
        var count = await _db.DownloadJobs.CountAsync();
        count.Should().Be(3);
    }

    [Fact]
    public async Task StartAsync_QuotaExceeded_DoesNotWriteToDatabase()
    {
        _quotaFacade.CheckAsync("user1", Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var request = new StartDownloadRequest(
            "https://youtube.com/watch?v=abc",
            "user1",
            "Mp3");

        var act = () => _sut.StartAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*quota*");

        var count = await _db.DownloadJobs.CountAsync();
        count.Should().Be(0);
    }
}
