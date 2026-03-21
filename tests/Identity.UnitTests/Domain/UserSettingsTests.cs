using MusicGrabber.Modules.Identity.Domain;

namespace MusicGrabber.Modules.Identity.UnitTests.Domain;

public sealed class UserSettingsTests
{
    [Fact]
    public void Constructor_DefaultValues_SetsExpectedDefaults()
    {
        var settings = new UserSettings();

        settings.Id.Should().NotBe(Guid.Empty);
        settings.DefaultFormat.Should().Be("Mp3");
        settings.EnableNormalization.Should().BeTrue();
        settings.NormalizationLufs.Should().Be(-14);
        settings.EmailNotifications.Should().BeTrue();
        settings.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void UserSettings_SetProperties_ValuesAreStored()
    {
        var settings = new UserSettings
        {
            UserId = "user@example.com",
            DefaultFormat = "Flac",
            EnableNormalization = false,
            NormalizationLufs = -16,
            EmailNotifications = false
        };

        settings.UserId.Should().Be("user@example.com");
        settings.DefaultFormat.Should().Be("Flac");
        settings.EnableNormalization.Should().BeFalse();
        settings.NormalizationLufs.Should().Be(-16);
        settings.EmailNotifications.Should().BeFalse();
    }
}
