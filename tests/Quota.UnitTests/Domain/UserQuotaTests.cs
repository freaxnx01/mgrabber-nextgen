using MusicGrabber.Modules.Quota.Domain;

namespace MusicGrabber.Modules.Quota.UnitTests.Domain;

public sealed class UserQuotaTests
{
    [Fact]
    public void Constructor_DefaultValues_SetsExpectedDefaults()
    {
        var quota = new UserQuota();

        quota.Id.Should().NotBe(Guid.Empty);
        quota.QuotaBytes.Should().Be(1_073_741_824L); // 1 GB
        quota.UsedBytes.Should().Be(0L);
        quota.FileCount.Should().Be(0);
        quota.CurrentThreshold.Should().Be("Normal");
        quota.LastEmailSentAt.Should().BeNull();
        quota.LastEmailThreshold.Should().BeNull();
        quota.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void UserQuota_SetProperties_ValuesAreStored()
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var quota = new UserQuota
        {
            Id = id,
            UserId = "user@example.com",
            QuotaBytes = 2_147_483_648L, // 2 GB
            UsedBytes = 500_000_000L,
            FileCount = 42,
            CurrentThreshold = "Warning",
            LastEmailSentAt = now,
            LastEmailThreshold = "Warning",
            UpdatedAt = now
        };

        quota.Id.Should().Be(id);
        quota.UserId.Should().Be("user@example.com");
        quota.QuotaBytes.Should().Be(2_147_483_648L);
        quota.UsedBytes.Should().Be(500_000_000L);
        quota.FileCount.Should().Be(42);
        quota.CurrentThreshold.Should().Be("Warning");
        quota.LastEmailSentAt.Should().Be(now);
        quota.LastEmailThreshold.Should().Be("Warning");
        quota.UpdatedAt.Should().Be(now);
    }

    [Theory]
    [InlineData(0L, 100L, "Normal")]    // 0%
    [InlineData(69L, 100L, "Normal")]   // 69%
    [InlineData(70L, 100L, "Warning")]  // 70%
    [InlineData(84L, 100L, "Warning")]  // 84%
    [InlineData(85L, 100L, "Critical")] // 85%
    [InlineData(99L, 100L, "Critical")] // 99%
    [InlineData(100L, 100L, "Blocked")] // 100%
    public void CalculateThreshold_VariousUsageLevels_ReturnsCorrectThreshold(
        long usedBytes, long quotaBytes, string expectedThreshold)
    {
        var result = UserQuota.CalculateThreshold(usedBytes, quotaBytes);

        result.Should().Be(expectedThreshold);
    }

    [Fact]
    public void CalculateThreshold_ExactlyAtWarning_ReturnsWarning()
    {
        // Use a quota that divides cleanly: 100 bytes, 70 used = exactly 70%
        var result = UserQuota.CalculateThreshold(70L, 100L);

        result.Should().Be("Warning");
    }

    [Fact]
    public void CalculateThreshold_ExactlyAtCritical_ReturnsCritical()
    {
        // 100 bytes, 85 used = exactly 85%
        var result = UserQuota.CalculateThreshold(85L, 100L);

        result.Should().Be("Critical");
    }

    [Fact]
    public void CalculateThreshold_ExactlyAtBlocked_ReturnsBlocked()
    {
        // 100%
        var result = UserQuota.CalculateThreshold(1_073_741_824L, 1_073_741_824L);

        result.Should().Be("Blocked");
    }

    [Fact]
    public void HasSpaceFor_SufficientSpace_ReturnsTrue()
    {
        var quota = new UserQuota
        {
            QuotaBytes = 1_073_741_824L,
            UsedBytes = 100_000_000L
        };

        quota.HasSpaceFor(50_000_000L).Should().BeTrue();
    }

    [Fact]
    public void HasSpaceFor_InsufficientSpace_ReturnsFalse()
    {
        var quota = new UserQuota
        {
            QuotaBytes = 1_073_741_824L,
            UsedBytes = 1_050_000_000L
        };

        quota.HasSpaceFor(50_000_000L).Should().BeFalse();
    }

    [Fact]
    public void HasSpaceFor_ExactlyFits_ReturnsTrue()
    {
        var quota = new UserQuota
        {
            QuotaBytes = 1_000L,
            UsedBytes = 500L
        };

        quota.HasSpaceFor(500L).Should().BeTrue();
    }
}
