using MusicGrabber.Modules.Quota.Domain;

namespace MusicGrabber.Modules.Quota.UnitTests.Domain;

public sealed class QuotaThresholdTests
{
    [Fact]
    public void Constants_AllExpectedValues_Exist()
    {
        QuotaThreshold.Normal.Should().Be("Normal");
        QuotaThreshold.Warning.Should().Be("Warning");
        QuotaThreshold.Critical.Should().Be("Critical");
        QuotaThreshold.Blocked.Should().Be("Blocked");
    }

    [Fact]
    public void WarningPercent_Is70()
    {
        QuotaThreshold.WarningPercent.Should().Be(70);
    }

    [Fact]
    public void CriticalPercent_Is85()
    {
        QuotaThreshold.CriticalPercent.Should().Be(85);
    }

    [Fact]
    public void BlockedPercent_Is100()
    {
        QuotaThreshold.BlockedPercent.Should().Be(100);
    }
}
