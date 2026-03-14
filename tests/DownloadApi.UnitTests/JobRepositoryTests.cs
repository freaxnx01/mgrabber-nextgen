namespace DownloadApi.UnitTests;

public class JobRepositoryTests
{
    private readonly string _testDbPath = Path.Combine(Path.GetTempPath(), $"test-jobs-{Guid.NewGuid()}.db");
    
    private JobRepository CreateRepository()
    {
        var logger = Substitute.For<ILogger<JobRepository>>();
        return new JobRepository(_testDbPath, logger);
    }

    [Fact]
    public async Task CreateJobAsync_ValidInput_CreatesJobWithPendingStatus()
    {
        // Arrange
        var repo = CreateRepository();
        var userId = "test-user-123";
        var url = "https://youtube.com/watch?v=test123";
        var format = "mp3";

        // Act
        var job = await repo.CreateJobAsync(userId, url, format);

        // Assert
        job.Should().NotBeNull();
        job.UserId.Should().Be(userId);
        job.Url.Should().Be(url);
        job.Format.Should().Be(format);
        job.Status.Should().Be(JobStatus.Pending);
        job.VideoId.Should().Be("test123");
    }

    [Fact]
    public async Task CreateJobAsync_WithTitleAndAuthor_StoresMetadata()
    {
        // Arrange
        var repo = CreateRepository();
        var title = "Test Song";
        var author = "Test Artist";

        // Act
        var job = await repo.CreateJobAsync("user1", "https://youtube.com/watch?v=abc", "mp3", title, author);

        // Assert
        job.Title.Should().Be(title);
        job.Author.Should().Be(author);
    }

    [Fact]
    public async Task GetJobAsync_ExistingJob_ReturnsJob()
    {
        // Arrange
        var repo = CreateRepository();
        var created = await repo.CreateJobAsync("user1", "https://youtube.com/watch?v=abc", "mp3");

        // Act
        var retrieved = await repo.GetJobAsync(created.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetJobAsync_NonExistingJob_ReturnsNull()
    {
        // Arrange
        var repo = CreateRepository();

        // Act
        var result = await repo.GetJobAsync("non-existent-id");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateJobStatusAsync_CompletedStatus_SetsCompletedAt()
    {
        // Arrange
        var repo = CreateRepository();
        var job = await repo.CreateJobAsync("user1", "https://youtube.com/watch?v=abc", "mp3");

        // Act
        await repo.UpdateJobStatusAsync(job.Id, JobStatus.Completed, 100, "/path/to/file.mp3", 1024);
        var updated = await repo.GetJobAsync(job.Id);

        // Assert
        updated!.Status.Should().Be(JobStatus.Completed);
        updated.Progress.Should().Be(100);
        updated.FilePath.Should().Be("/path/to/file.mp3");
        updated.FileSizeBytes.Should().Be(1024);
        updated.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetUserJobsAsync_ReturnsOnlyUserJobs()
    {
        // Arrange
        var repo = CreateRepository();
        await repo.CreateJobAsync("user1", "https://youtube.com/watch?v=abc1", "mp3");
        await repo.CreateJobAsync("user1", "https://youtube.com/watch?v=abc2", "mp3");
        await repo.CreateJobAsync("user2", "https://youtube.com/watch?v=def", "mp3");

        // Act
        var user1Jobs = await repo.GetUserJobsAsync("user1");

        // Assert
        user1Jobs.Should().HaveCount(2);
        user1Jobs.All(j => j.UserId == "user1").Should().BeTrue();
    }

    [Fact]
    public async Task DeleteJobAsync_ExistingJob_RemovesJob()
    {
        // Arrange
        var repo = CreateRepository();
        var job = await repo.CreateJobAsync("user1", "https://youtube.com/watch?v=abc", "mp3");

        // Act
        await repo.DeleteJobAsync(job.Id);
        var deleted = await repo.GetJobAsync(job.Id);

        // Assert
        deleted.Should().BeNull();
    }

    public void Dispose()
    {
        // Cleanup test database
        if (File.Exists(_testDbPath))
        {
            File.Delete(_testDbPath);
        }
    }
}
