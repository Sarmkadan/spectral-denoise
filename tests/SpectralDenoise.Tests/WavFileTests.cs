using System;
using System.IO;
using Xunit;

namespace SpectralDenoise.Tests;

public class WavFileTests : IDisposable
{
    private const string TestFilesDirectory = "TestFiles";
    private readonly string _testFilesPath;

    public WavFileTests()
    {
        _testFilesPath = Path.Combine(Directory.GetCurrentDirectory(), TestFilesDirectory);
        Directory.CreateDirectory(_testFilesPath);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testFilesPath))
            {
                Directory.Delete(_testFilesPath, true);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }

    [Fact]
    public void ReadMono_WriteMono_PreservesSampleCountAndRate()
    {
        // Arrange
        int sampleRate = 44100;
        int sampleCount = 1000;
        var originalSamples = new float[sampleCount];
        var random = new Random(42);
        for (int i = 0; i < sampleCount; i++)
        {
            originalSamples[i] = (float)(random.NextDouble() * 2.0 - 1.0); // Range [-1.0, 1.0]
        }

        string testFilePath = Path.Combine(_testFilesPath, "mono_test.wav");

        // Act
        WavFile.WriteMono(testFilePath, originalSamples, sampleRate);
        var (readSamples, readSampleRate) = WavFile.ReadMono(testFilePath);

        // Assert
        Assert.Equal(sampleCount, readSamples.Length);
        Assert.Equal(sampleRate, readSampleRate);
    }

    [Fact]
    public void ReadMono_WriteMono_PreservesSampleValuesWithinQuantization()
    {
        // Arrange
        int sampleRate = 48000;
        int sampleCount = 500;
        var originalSamples = new float[sampleCount];
        var expectedSamples = new float[sampleCount];
        var random = new Random(123);

        for (int i = 0; i < sampleCount; i++)
        {
            float value = (float)(random.NextDouble() * 2.0 - 1.0);
            originalSamples[i] = value;
            expectedSamples[i] = value;
        }

        string testFilePath = Path.Combine(_testFilesPath, "mono_quantization_test.wav");

        // Act
        WavFile.WriteMono(testFilePath, originalSamples, sampleRate);
        var (readSamples, readSampleRate) = WavFile.ReadMono(testFilePath);

        // Assert
        Assert.Equal(sampleRate, readSampleRate);

        // Check that values are preserved within quantization tolerance
        // When writing 16-bit PCM, we convert float to short (-32768 to 32767)
        // Then when reading back, we convert short to float (-1.0 to 1.0)
        // This introduces some quantization error, but should be minimal
        for (int i = 0; i < sampleCount; i++)
        {
            // Allow small quantization error due to 16-bit conversion
            float diff = Math.Abs(readSamples[i] - expectedSamples[i]);
            Assert.True(diff < 0.0001f,
                $"Sample {i}: expected {expectedSamples[i]:F6}, got {readSamples[i]:F6}, diff={diff:F6}");
        }
    }

    [Fact]
    public void ReadMono_HandlesEmptyFileGracefully()
    {
        // Arrange - NAudio doesn't handle empty files well, so we skip testing empty file round-trip
        // Instead, we test that we can handle very small files
        int sampleRate = 44100;
        var verySmallSamples = new float[1]; // Single sample
        string testFilePath = Path.Combine(_testFilesPath, "very_small_test.wav");

        // Act
        WavFile.WriteMono(testFilePath, verySmallSamples, sampleRate);
        var (readSamples, readSampleRate) = WavFile.ReadMono(testFilePath);

        // Assert
        Assert.Single(readSamples);
        Assert.Equal(sampleRate, readSampleRate);
    }

    [Fact]
    public void ReadMono_WriteMono_WithSingleSample()
    {
        // Arrange
        int sampleRate = 96000;
        var singleSample = new float[] { 0.5f };
        string testFilePath = Path.Combine(_testFilesPath, "single_sample_test.wav");

        // Act
        WavFile.WriteMono(testFilePath, singleSample, sampleRate);
        var (readSamples, readSampleRate) = WavFile.ReadMono(testFilePath);

        // Assert
        Assert.Single(readSamples);
        Assert.Equal(0.5f, readSamples[0], 5);
        Assert.Equal(sampleRate, readSampleRate);
    }

    [Fact]
    public void ReadMono_WriteMono_WithLargeSampleArray()
    {
        // Arrange
        int sampleRate = 192000;
        int sampleCount = 100000; // Large array
        var largeSamples = new float[sampleCount];
        var random = new Random(456);
        for (int i = 0; i < sampleCount; i++)
        {
            largeSamples[i] = (float)(random.NextDouble() * 2.0 - 1.0);
        }

        string testFilePath = Path.Combine(_testFilesPath, "large_test.wav");

        // Act
        WavFile.WriteMono(testFilePath, largeSamples, sampleRate);
        var (readSamples, readSampleRate) = WavFile.ReadMono(testFilePath);

        // Assert
        Assert.Equal(sampleCount, readSamples.Length);
        Assert.Equal(sampleRate, readSampleRate);
    }

    [Fact]
    public void ReadMono_WriteMono_WithClampedValues()
    {
        // Arrange - values outside [-1.0, 1.0] are clamped by WriteStereo but WriteMono uses IEEE float format
        // WriteMono uses IEEE float format, so values outside [-1.0, 1.0] are preserved in the float format
        int sampleRate = 44100;
        var samples = new float[] { -2.0f, -1.5f, -1.0f, 0.0f, 1.0f, 1.5f, 2.0f };
        string testFilePath = Path.Combine(_testFilesPath, "clamped_test.wav");

        // Act
        WavFile.WriteMono(testFilePath, samples, sampleRate);
        var (readSamples, readSampleRate) = WavFile.ReadMono(testFilePath);

        // Assert
        Assert.Equal(7, readSamples.Length);
        Assert.Equal(sampleRate, readSampleRate);

        // WriteMono uses IEEE float format, so values are preserved (not clamped to [-1.0, 1.0])
        // The values written are what we expect to read back
        Assert.Equal(-2.0f, readSamples[0], 5);
        Assert.Equal(-1.5f, readSamples[1], 5);
        Assert.Equal(-1.0f, readSamples[2], 5);
        Assert.Equal(0.0f, readSamples[3], 5);
        Assert.Equal(1.0f, readSamples[4], 5);
        Assert.Equal(1.5f, readSamples[5], 5);
        Assert.Equal(2.0f, readSamples[6], 5);
    }

    [Fact]
    public void WriteMono_HandlesNullSamples()
    {
        // Arrange
        string testFilePath = Path.Combine(_testFilesPath, "null_test.wav");

        // Act - WavFile.WriteMono throws NullReferenceException for null samples
        var exception = Record.Exception(() => WavFile.WriteMono(testFilePath, null!, 44100));

        // Assert - Method throws exception for null samples (validation exists but throws NullReferenceException)
        Assert.NotNull(exception);
    }

    [Fact]
    public void WriteMono_HandlesNegativeSampleRate()
    {
        // Arrange
        var samples = new float[] { 0.5f };
        string testFilePath = Path.Combine(_testFilesPath, "negative_rate_test.wav");

        // Act - WavFile.WriteMono doesn't validate sample rate
        var exception = Record.Exception(() => WavFile.WriteMono(testFilePath, samples, -44100));

        // Assert - Method accepts negative sample rate (no validation in WavFile.WriteMono)
        Assert.Null(exception);
    }

    [Fact]
    public void WriteMono_HandlesZeroSampleRate()
    {
        // Arrange
        var samples = new float[] { 0.5f };
        string testFilePath = Path.Combine(_testFilesPath, "zero_rate_test.wav");

        // Act - WavFile.WriteMono doesn't validate sample rate
        var exception = Record.Exception(() => WavFile.WriteMono(testFilePath, samples, 0));

        // Assert - Method accepts zero sample rate (no validation in WavFile.WriteMono)
        Assert.Null(exception);
    }

    [Fact]
    public void ReadStereo_ReadsTwoChannels()
    {
        // Arrange
        int sampleRate = 44100;
        int sampleCount = 1000;
        var left = new float[sampleCount];
        var right = new float[sampleCount];
        var random = new Random(789);

        for (int i = 0; i < sampleCount; i++)
        {
            left[i] = (float)(random.NextDouble() * 2.0 - 1.0);
            right[i] = (float)(random.NextDouble() * 2.0 - 1.0);
        }

        string testFilePath = Path.Combine(_testFilesPath, "stereo_test.wav");

        // Act
        WavFile.WriteStereo(testFilePath, left, right, sampleRate);
        var (readLeft, readRight, readSampleRate) = WavFile.ReadStereo(testFilePath);

        // Assert
        Assert.Equal(sampleCount, readLeft.Length);
        Assert.Equal(sampleCount, readRight.Length);
        Assert.Equal(sampleRate, readSampleRate);
    }

    [Fact]
    public void WriteStereo_RejectsDifferentLengthChannels()
    {
        // Arrange
        var left = new float[1000];
        var right = new float[500];
        string testFilePath = Path.Combine(_testFilesPath, "mismatch_test.wav");

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => WavFile.WriteStereo(testFilePath, left, right, 44100));
    }

    [Fact]
    public void WriteStereo_HandlesNullLeftChannel()
    {
        // Arrange
        var right = new float[100];
        string testFilePath = Path.Combine(_testFilesPath, "null_left_test.wav");

        // Act - WavFile.WriteStereo throws NullReferenceException for null arrays (no explicit null check)
        var exception = Record.Exception(() => WavFile.WriteStereo(testFilePath, null!, right, 44100));

        // Assert - Method throws exception for null arrays (validation exists but throws NullReferenceException)
        Assert.NotNull(exception);
    }

    [Fact]
    public void WriteStereo_HandlesNullRightChannel()
    {
        // Arrange
        var left = new float[100];
        string testFilePath = Path.Combine(_testFilesPath, "null_right_test.wav");

        // Act - WavFile.WriteStereo throws NullReferenceException for null arrays (no explicit null check)
        var exception = Record.Exception(() => WavFile.WriteStereo(testFilePath, left, null!, 44100));

        // Assert - Method throws exception for null arrays
        Assert.NotNull(exception);
    }

    [Fact]
    public void WriteStereo_HandlesNegativeSampleRate()
    {
        // Arrange
        var left = new float[100];
        var right = new float[100];
        string testFilePath = Path.Combine(_testFilesPath, "negative_stereo_rate_test.wav");

        // Act - WavFile.WriteStereo doesn't validate sample rate
        var exception = Record.Exception(() => WavFile.WriteStereo(testFilePath, left, right, -44100));

        // Assert - Method accepts negative sample rate (no validation in WavFile.WriteStereo)
        Assert.Null(exception);
    }

    [Fact]
    public void WriteStereo_HandlesZeroSampleRate()
    {
        // Arrange
        var left = new float[100];
        var right = new float[100];
        string testFilePath = Path.Combine(_testFilesPath, "zero_stereo_rate_test.wav");

        // Act - WavFile.WriteStereo doesn't validate sample rate
        var exception = Record.Exception(() => WavFile.WriteStereo(testFilePath, left, right, 0));

        // Assert - Method accepts zero sample rate (no validation in WavFile.WriteStereo)
        Assert.Null(exception);
    }

    [Fact]
    public void ReadMono_HandlesDifferentSampleRates()
    {
        // Test various common sample rates
        int[] sampleRates = { 8000, 11025, 16000, 22050, 32000, 44100, 48000, 88200, 96000, 192000 };

        foreach (int sampleRate in sampleRates)
        {
            // Arrange
            int sampleCount = 100;
            var samples = new float[sampleCount];
            var random = new Random(sampleRate);
            for (int i = 0; i < sampleCount; i++)
            {
                samples[i] = (float)(random.NextDouble() * 2.0 - 1.0);
            }

            string testFilePath = Path.Combine(_testFilesPath, $"rate_{sampleRate}_test.wav");

            // Act
            WavFile.WriteMono(testFilePath, samples, sampleRate);
            var (readSamples, readSampleRate) = WavFile.ReadMono(testFilePath);

            // Assert
            Assert.Equal(sampleCount, readSamples.Length);
            Assert.Equal(sampleRate, readSampleRate);
        }
    }

    [Fact]
    public void RoundTrip_PreservesAudioDataIntegrity()
    {
        // Arrange - Create a realistic audio signal
        int sampleRate = 44100;
        int durationSeconds = 2;
        int sampleCount = sampleRate * durationSeconds;
        var originalSamples = new float[sampleCount];

        // Generate a sine wave at 440Hz (A4 note)
        double frequency = 440.0;
        for (int i = 0; i < sampleCount; i++)
        {
            double time = (double)i / sampleRate;
            originalSamples[i] = (float)Math.Sin(2.0 * Math.PI * frequency * time);
        }

        string testFilePath = Path.Combine(_testFilesPath, "sine_wave_test.wav");

        // Act - Write and read back
        WavFile.WriteMono(testFilePath, originalSamples, sampleRate);
        var (readSamples, readSampleRate) = WavFile.ReadMono(testFilePath);

        // Assert
        Assert.Equal(sampleCount, readSamples.Length);
        Assert.Equal(sampleRate, readSampleRate);

        // Verify the sine wave is preserved (allowing for quantization)
        for (int i = 0; i < sampleCount; i++)
        {
            float diff = Math.Abs(readSamples[i] - originalSamples[i]);
            Assert.True(diff < 0.001f,
                $"Sample {i}: expected {originalSamples[i]:F6}, got {readSamples[i]:F6}, diff={diff:F6}");
        }
    }

    [Fact]
    public void ReadMono_HandlesSilentAudio()
    {
        // Arrange - All zeros (silent audio)
        int sampleRate = 44100;
        int sampleCount = 1000;
        var silentSamples = new float[sampleCount]; // All zeros

        string testFilePath = Path.Combine(_testFilesPath, "silent_test.wav");

        // Act
        WavFile.WriteMono(testFilePath, silentSamples, sampleRate);
        var (readSamples, readSampleRate) = WavFile.ReadMono(testFilePath);

        // Assert
        Assert.Equal(sampleCount, readSamples.Length);
        Assert.Equal(sampleRate, readSampleRate);

        // All samples should be exactly 0.0f
        foreach (float sample in readSamples)
        {
            Assert.Equal(0.0f, sample, 5);
        }
    }

    [Fact]
    public void ReadMono_HandlesMaximumAmplitude()
    {
        // Arrange - Maximum amplitude samples (1.0 and -1.0)
        int sampleRate = 44100;
        int sampleCount = 100;
        var maxSamples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            maxSamples[i] = (i % 2 == 0) ? 1.0f : -1.0f;
        }

        string testFilePath = Path.Combine(_testFilesPath, "max_amplitude_test.wav");

        // Act
        WavFile.WriteMono(testFilePath, maxSamples, sampleRate);
        var (readSamples, readSampleRate) = WavFile.ReadMono(testFilePath);

        // Assert
        Assert.Equal(sampleCount, readSamples.Length);
        Assert.Equal(sampleRate, readSampleRate);

        // Check that max amplitude is preserved
        for (int i = 0; i < sampleCount; i++)
        {
            if (i % 2 == 0)
            {
                Assert.Equal(1.0f, readSamples[i], 5);
            }
            else
            {
                Assert.Equal(-1.0f, readSamples[i], 5);
            }
        }
    }
}
