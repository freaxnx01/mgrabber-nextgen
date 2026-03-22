using MusicGrabber.Modules.Identity.Domain;

namespace MusicGrabber.Modules.Identity.UnitTests.Domain;

public sealed class WhitelistEntryTests
{
    [Fact]
    public void Constructor_DefaultValues_SetsExpectedDefaults()
    {
        var entry = new WhitelistEntry();

        entry.Id.Should().NotBe(Guid.Empty);
        entry.Role.Should().Be("User");
        entry.IsActive.Should().BeTrue();
        entry.WelcomeEmailSent.Should().BeFalse();
        entry.AddedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void WhitelistEntry_SetProperties_ValuesAreStored()
    {
        var entry = new WhitelistEntry
        {
            UserId = "user@example.com",
            Role = "Admin",
            AddedBy = "admin@example.com",
            IsActive = false,
            WelcomeEmailSent = true
        };

        entry.UserId.Should().Be("user@example.com");
        entry.Role.Should().Be("Admin");
        entry.AddedBy.Should().Be("admin@example.com");
        entry.IsActive.Should().BeFalse();
        entry.WelcomeEmailSent.Should().BeTrue();
    }
}
