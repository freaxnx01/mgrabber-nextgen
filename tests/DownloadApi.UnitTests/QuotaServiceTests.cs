using DownloadApi.Data;
using DownloadApi.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DownloadApi.UnitTests;

public class QuotaServiceTests
{
    private readonly JobRepository _repository;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<QuotaService> _logger;
    private readonly QuotaService _service;

    public QuotaServiceTests()
    {
        _repository = Substitute.For<JobRepository>("test.db", Substitute.For<ILogger<JobRepository>>());
        _emailService = Substitute.For<IEmailService>();
        _configuration = Substitute.For<IConfiguration>();
        _logger = Substitute.For<ILogger<QuotaService>>();
        
        // Default config: 1GB quota, 80%/90%/95% thresholds
        _configuration["Quota:MaxBytesPerUser"].Returns("1073741824");
        _configuration["Quota:WarningThreshold"].Returns("0.80");
        _configuration["Quota:CriticalThreshold"].Returns("0.90");
        _configuration["Quota:BlockedThreshold"].Returns("0.95");
        
        _service = new QuotaService(_repository, _emailService, _configuration, _logger);
    }

    [Fact]
    public async Task GetUserQuotaAsync_NoJobs_ReturnsZeroUsage()
    {
        // Arrange
        var userId = "test@example.com";
        _repository.GetUserJobsAsync(userId).Returns(Task.FromResult(new List<Job>()));

        // Act
        var result = await _service.GetUserQuotaAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.TotalBytesUsed.Should().Be(0);
        result.PercentageUsed.Should().Be(0);
        result.FileCount.Should().Be(0);
        result.Threshold.Should().Be(QuotaThreshold.Normal);
    }

    [Theory]
    [InlineData(100, 100 * 1024 * 1024, 0.0093, QuotaThreshold.Normal)]     // 100 MB < 80%
    [InlineData(5, 850 * 1024 * 1024, 0.79, QuotaThreshold.Normal)]           // 850 MB < 80%
    [InlineData(5, 860 * 1024 * 1024, 0.80, QuotaThreshold.Warning)]          // 860 MB >= 80%
    [InlineData(5, 970 * 1024 * 1024, 0.90, QuotaThreshold.Critical)]         // 970 MB >= 90%
    [InlineData(5, 1024 * 1024 * 1024, 0.95, QuotaThreshold.Blocked)]         // 1 GB >= 95%
    public async Task GetUserQuotaAsync_VariousUsages_ReturnsCorrectThreshold(
        int fileCount, long bytesUsed, double expectedPercent, QuotaThreshold expectedThreshold)
    {
        // Arrange
        var userId = "test@example.com";
        var jobs = Enumerable.Range(0, fileCount)
            .Select(i => new Job 
            { 
                Status = JobStatus.Completed, 
                FileSizeBytes = bytesUsed / fileCount 
            })
            .ToList();
        
        _repository.GetUserJobsAsync(userId).Returns(Task.FromResult(jobs));

        // Act
        var result = await _service.GetUserQuotaAsync(userId);

        // Assert
        result.PercentageUsed.Should().BeApproximately(expectedPercent * 100, 0.5);
        result.Threshold.Should().Be(expectedThreshold);
    }

    [Fact]
    public async Task WouldExceedQuotaAsync_WithEnoughSpace_ReturnsFalse()
    {
        // Arrange
        var userId = "test@example.com";
        var jobs = new List<Job>
        {
            new() { Status = JobStatus.Completed, FileSizeBytes = 100 * 1024 * 1024 } // 100 MB used
        };
        _repository.GetUserJobsAsync(userId).Returns(Task.FromResult(jobs));

        // Act
        var result = await _service.WouldExceedQuotaAsync(userId, 100 * 1024 * 1024); // Adding 100 MB

        // Assert
        result.Should().BeFalse(); // 200 MB < 1 GB
    }

    [Fact]
    public async Task WouldExceedQuotaAsync_WithNotEnoughSpace_ReturnsTrue()
    {
        // Arrange
        var userId = "test@example.com";
        var jobs = new List<Job>
        {
            new() { Status = JobStatus.Completed, FileSizeBytes = 950 * 1024 * 1024 } // 950 MB used
        };
        _repository.GetUserJobsAsync(userId).Returns(Task.FromResult(jobs));

        // Act
        var result = await _service.WouldExceedQuotaAsync(userId, 100 * 1024 * 1024); // Adding 100 MB

        // Assert
        result.Should().BeTrue(); // 1050 MB > 1 GB
    }

    [Fact]
    public async Task CheckAndNotifyQuotaAsync_NormalThreshold_DoesNotSendEmail()
    {
        // Arrange
        var userId = "test@example.com";
        var jobs = new List<Job>
        {
            new() { Status = JobStatus.Completed, FileSizeBytes = 100 * 1024 * 1024 }
        };
        _repository.GetUserJobsAsync(userId).Returns(Task.FromResult(jobs));

        // Act
        await _service.CheckAndNotifyQuotaAsync(userId);

        // Assert
        await _emailService.DidNotReceive().SendQuotaWarningAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<double>());
        await _emailService.DidNotReceive().SendQuotaCriticalAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<double>());
        await _emailService.DidNotReceive().SendQuotaBlockedAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<double>());
    }

    [Fact]
    public async Task CheckAndNotifyQuotaAsync_WarningThreshold_SendsWarningEmail()
    {
        // Arrange
        var userId = "test@example.com";
        var jobs = new List<Job>
        {
            new() { Status = JobStatus.Completed, FileSizeBytes = (long)(0.85 * 1073741824) } // 85% usage
        };
        _repository.GetUserJobsAsync(userId).Returns(Task.FromResult(jobs));

        // Act
        await _service.CheckAndNotifyQuotaAsync(userId);

        // Assert
        await _emailService.Received(1).SendQuotaWarningAsync(userId, userId, Arg.Any<double>());
    }

    [Fact]
    public async Task CheckAndNotifyQuotaAsync_CriticalThreshold_SendsCriticalEmail()
    {
        // Arrange
        var userId = "test@example.com";
        var jobs = new List<Job>
        {
            new() { Status = JobStatus.Completed, FileSizeBytes = (long)(0.92 * 1073741824) } // 92% usage
        };
        _repository.GetUserJobsAsync(userId).Returns(Task.FromResult(jobs));

        // Act
        await _service.CheckAndNotifyQuotaAsync(userId);

        // Assert
        await _emailService.Received(1).SendQuotaCriticalAsync(userId, userId, Arg.Any<double>());
    }

    [Fact]
    public async Task CheckAndNotifyQuotaAsync_BlockedThreshold_SendsBlockedEmail()
    {
        // Arrange
        var userId = "test@example.com";
        var jobs = new List<Job>
        {
            new() { Status = JobStatus.Completed, FileSizeBytes = (long)(0.96 * 1073741824) } // 96% usage
        };
        _repository.GetUserJobsAsync(userId).Returns(Task.FromResult(jobs));

        // Act
        await _service.CheckAndNotifyQuotaAsync(userId);

        // Assert
        await _emailService.Received(1).SendQuotaBlockedAsync(userId, userId, Arg.Any<double>());
    }

    [Fact]
    public async Task CheckAndNotifyQuotaAsync_InvalidUserId_DoesNotSendEmail()
    {
        // Arrange
        var userId = "not-an-email-userid";
        var jobs = new List<Job>
        {
            new() { Status = JobStatus.Completed, FileSizeBytes = (long)(0.85 * 1073741824) }
        };
        _repository.GetUserJobsAsync(userId).Returns(Task.FromResult(jobs));

        // Act
        await _service.CheckAndNotifyQuotaAsync(userId);

        // Assert
        await _emailService.DidNotReceive().SendQuotaWarningAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<double>());
    }

    [Fact]
    public async Task GetUserQuotaAsync_WithPendingAndFailedJobs_IgnoresNonCompleted()
    {
        // Arrange
        var userId = "test@example.com";
        var jobs = new List<Job>
        {
            new() { Status = JobStatus.Completed, FileSizeBytes = 100 * 1024 * 1024 },
            new() { Status = JobStatus.Pending, FileSizeBytes = 200 * 1024 * 1024 },
            new() { Status = JobStatus.Failed, FileSizeBytes = 300 * 1024 * 1024 },
            new() { Status = JobStatus.Processing, FileSizeBytes = 400 * 1024 * 1024 }
        };
        _repository.GetUserJobsAsync(userId).Returns(Task.FromResult(jobs));

        // Act
        var result = await _service.GetUserQuotaAsync(userId);

        // Assert
        result.TotalBytesUsed.Should().Be(100 * 1024 * 1024); // Only completed counts
        result.FileCount.Should().Be(1);
    }
}
