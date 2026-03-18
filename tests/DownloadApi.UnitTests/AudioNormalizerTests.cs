using DownloadApi;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DownloadApi.UnitTests;

public class AudioNormalizerTests
{
    private readonly ILogger<AudioNormalizer> _logger;
    private readonly AudioNormalizer _normalizer;

    public AudioNormalizerTests()
    {
        _logger = Substitute.For<ILogger<AudioNormalizer>>();
        _normalizer = new AudioNormalizer(_logger);
    }

    [Fact]
    public void NormalizationOptions_DefaultValues_AreCorrect()
    {
        // Act
        var options = new NormalizationOptions();

        // Assert
        options.TargetLoudness.Should().Be(-14); // YouTube/Spotify standard
        options.TruePeak.Should().Be(-1);
        options.LoudnessRange.Should().Be(11);
    }

    [Fact]
    public void NormalizationOptions_CanCustomizeValues()
    {
        // Act
        var options = new NormalizationOptions
        {
            TargetLoudness = -23, // EBU R128 standard
            TruePeak = -2,
            LoudnessRange = 15
        };

        // Assert
        options.TargetLoudness.Should().Be(-23);
        options.TruePeak.Should().Be(-2);
        options.LoudnessRange.Should().Be(15);
    }

    [Fact]
    public void NormalizationResult_DefaultState_IsFailed()
    {
        // Act
        var result = new NormalizationResult();

        // Assert
        result.Success.Should().BeFalse();
        result.OutputPath.Should().BeNull();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void MeasuredValues_HasExpectedProperties()
    {
        // Act
        var values = new MeasuredValues
        {
            MeasuredI = -16.5,
            MeasuredTP = -0.8,
            MeasuredLRA = 8.2,
            MeasuredThresh = -27.3,
            Offset = 1.2
        };

        // Assert
        values.MeasuredI.Should().Be(-16.5);
        values.MeasuredTP.Should().Be(-0.8);
        values.MeasuredLRA.Should().Be(8.2);
        values.MeasuredThresh.Should().Be(-27.3);
        values.Offset.Should().Be(1.2);
    }

    [Fact]
    public void NormalizationResult_Success_HasCorrectValues()
    {
        // Arrange
        var result = new NormalizationResult
        {
            Success = true,
            OutputPath = "/path/to/output.mp3",
            OutputSizeBytes = 1024000,
            MeasuredIntegratedLoudness = -14.2,
            MeasuredTruePeak = -1.1,
            MeasuredLoudnessRange = 11.5
        };

        // Assert
        result.Success.Should().BeTrue();
        result.OutputPath.Should().Be("/path/to/output.mp3");
        result.OutputSizeBytes.Should().Be(1024000);
        result.MeasuredIntegratedLoudness.Should().Be(-14.2);
        result.MeasuredTruePeak.Should().Be(-1.1);
        result.MeasuredLoudnessRange.Should().Be(11.5);
    }
}
