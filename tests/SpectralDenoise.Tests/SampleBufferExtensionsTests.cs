using System;
using Xunit;
using SpectralDenoise;

namespace SpectralDenoise.Tests;

public class SampleBufferExtensionsTests
{
    [Fact]
    public void NormalizePeak_NormalizesCorrectly()
    {
        float[] samples = { 0.1f, 0.2f, 0.4f };
        samples.NormalizePeak(0.8f);
        // max was 0.4. factor = 0.8 / 0.4 = 2.0.
        // samples should be { 0.2f, 0.4f, 0.8f }
        Assert.Equal(0.2f, samples[0], 5);
        Assert.Equal(0.4f, samples[1], 5);
        Assert.Equal(0.8f, samples[2], 5);
    }

    [Fact]
    public void RmsDb_CalculatesCorrectly()
    {
        float[] samples = { 0.5f, 0.5f, 0.5f, 0.5f };
        // rms = sqrt((0.25*4)/4) = 0.5
        // 20 * log10(0.5) = 20 * -0.30103 = -6.0206
        float db = samples.RmsDb();
        Assert.Equal(-6.0206f, db, 2);
    }

    [Fact]
    public void ApplyGainDb_AppliesGainCorrectly()
    {
        float[] samples = { 0.5f };
        // 6dB = factor 2.0
        samples.ApplyGainDb(6.0206f); // ~2x
        Assert.Equal(1.0f, samples[0], 2);
    }

    [Fact]
    public void TrimSilence_TrimsCorrectly()
    {
        float[] samples = { 0.0001f, 0.0001f, 0.5f, 0.6f, 0.0001f };
        // -50dB = 10^(-50/20) = 10^-2.5 = 0.00316
        float[] trimmed = samples.TrimSilence(-50f);
        Assert.Equal(2, trimmed.Length);
        Assert.Equal(0.5f, trimmed[0]);
        Assert.Equal(0.6f, trimmed[1]);
    }
}
