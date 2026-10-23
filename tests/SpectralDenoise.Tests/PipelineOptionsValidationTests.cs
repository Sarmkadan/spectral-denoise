using System;
using SpectralDenoise;
using Xunit;

namespace SpectralDenoise.Tests;

public class PipelineOptionsValidationTests
{
    private static PipelineOptions ValidOptions => new(44_100, 1024, 256, 2.0, 0.02);

    [Fact]
    public void Validate_DefaultOptions_ReturnsNoErrors()
    {
        Assert.Empty(PipelineOptions.Default.Validate());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-44_100)]
    public void Validate_NonPositiveSampleRate_ReportsSampleRate(int sampleRate)
    {
        var errors = (ValidOptions with { SampleRate = sampleRate }).Validate();

        Assert.Single(errors);
        Assert.Contains("SampleRate", errors[0]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(128)]
    [InlineData(1024)]
    [InlineData(8192)]
    public void Validate_PowerOfTwoFrameSize_ReturnsNoErrors(int frameSize)
    {
        var options = ValidOptions with { FrameSize = frameSize, Hop = 1 };

        Assert.Empty(options.Validate());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1024)]
    [InlineData(3)]
    [InlineData(1000)]
    [InlineData(1023)]
    public void Validate_FrameSizeNotPowerOfTwo_ReportsFrameSize(int frameSize)
    {
        var errors = (ValidOptions with { FrameSize = frameSize, Hop = 1 }).Validate();

        Assert.Contains(errors, error => error.Contains("FrameSize"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(256)]
    [InlineData(1024)]
    public void Validate_HopWithinZeroToFrameSize_ReturnsNoErrors(int hop)
    {
        var options = ValidOptions with { Hop = hop };

        Assert.Empty(options.Validate());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-256)]
    [InlineData(1025)]
    [InlineData(2048)]
    public void Validate_HopOutsideZeroToFrameSize_ReportsHop(int hop)
    {
        var errors = (ValidOptions with { Hop = hop }).Validate();

        Assert.Single(errors);
        Assert.Contains("Hop", errors[0]);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(2.0)]
    [InlineData(10.0)]
    public void Validate_OverSubtractionAtLeastOne_ReturnsNoErrors(double factor)
    {
        var options = ValidOptions with { OverSubtractionFactor = factor };

        Assert.Empty(options.Validate());
    }

    [Theory]
    [InlineData(0.99)]
    [InlineData(0.0)]
    [InlineData(-2.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Validate_OverSubtractionNotFiniteOrBelowOne_ReportsFactor(double factor)
    {
        var errors = (ValidOptions with { OverSubtractionFactor = factor }).Validate();

        Assert.Single(errors);
        Assert.Contains("OverSubtractionFactor", errors[0]);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.02)]
    [InlineData(0.999)]
    public void Validate_SpectralFloorInZeroToOne_ReturnsNoErrors(double floor)
    {
        var options = ValidOptions with { SpectralFloor = floor };

        Assert.Empty(options.Validate());
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.0)]
    [InlineData(1.5)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Validate_SpectralFloorOutsideZeroToOne_ReportsFloor(double floor)
    {
        var errors = (ValidOptions with { SpectralFloor = floor }).Validate();

        Assert.Single(errors);
        Assert.Contains("SpectralFloor", errors[0]);
    }

    [Fact]
    public void Validate_EveryRuleViolated_ReturnsOneErrorPerRule()
    {
        var options = new PipelineOptions(0, 1000, 2000, 0.5, 1.0);

        var errors = options.Validate();

        Assert.Equal(5, errors.Count);
        Assert.Contains(errors, error => error.Contains("SampleRate"));
        Assert.Contains(errors, error => error.Contains("FrameSize"));
        Assert.Contains(errors, error => error.Contains("Hop"));
        Assert.Contains(errors, error => error.Contains("OverSubtractionFactor"));
        Assert.Contains(errors, error => error.Contains("SpectralFloor"));
    }

    [Fact]
    public void IsValid_DefaultOptions_ReturnsTrue()
    {
        Assert.True(PipelineOptions.Default.IsValid());
    }

    [Fact]
    public void IsValid_InvalidOptions_ReturnsFalse()
    {
        Assert.False((ValidOptions with { Hop = 0 }).IsValid());
    }

    [Fact]
    public void EnsureValid_ValidOptions_DoesNotThrow()
    {
        var exception = Record.Exception(() => PipelineOptions.Default.EnsureValid());

        Assert.Null(exception);
    }

    [Fact]
    public void EnsureValid_SingleViolation_ThrowsWithOneError()
    {
        var options = ValidOptions with { SpectralFloor = 1.0 };

        var exception = Assert.Throws<InvalidPipelineOptionsException>(() => options.EnsureValid());

        Assert.Single(exception.Errors);
        Assert.Contains("SpectralFloor", exception.Message);
    }

    [Fact]
    public void EnsureValid_MultipleViolations_ThrowsAggregatedExceptionListingEveryError()
    {
        var options = new PipelineOptions(-1, 1000, 0, 0.5, -0.1);

        var exception = Assert.Throws<InvalidPipelineOptionsException>(() => options.EnsureValid());

        Assert.Equal(5, exception.Errors.Count);
        Assert.Contains("(5 problem(s))", exception.Message);
        Assert.Contains("SampleRate", exception.Message);
        Assert.Contains("FrameSize", exception.Message);
        Assert.Contains("Hop", exception.Message);
        Assert.Contains("OverSubtractionFactor", exception.Message);
        Assert.Contains("SpectralFloor", exception.Message);
    }

    [Fact]
    public void EnsureValid_MultipleViolations_IsAnArgumentException()
    {
        var options = new PipelineOptions(0, 1000, 0, 0.5, 1.0);

        Assert.ThrowsAny<ArgumentException>(() => options.EnsureValid());
    }
}
