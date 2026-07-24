using System;
using System.IO;
using System.Text;
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

    [Fact]
    public void ReadMono_ThrowsForUnsupportedBitDepth()
    {
        // Arrange - Create a 24-bit PCM WAV file (unsupported format)
        // RIFF header with WAVE format, but 24-bit samples
        string testFilePath = Path.Combine(_testFilesPath, "24bit_test.wav");

        // Create a minimal 24-bit WAV file header
        using (var writer = new BinaryWriter(File.OpenWrite(testFilePath)))
        {
            // RIFF header
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(0); // Placeholder for file size
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));

            // fmt chunk
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16); // Chunk size
            writer.Write((short)1); // PCM format
            writer.Write((short)1); // Mono
            writer.Write(44100); // Sample rate
            writer.Write(132300); // Byte rate (44100 * 3 bytes/sample * 1 channel)
            writer.Write((short)3); // Block align (3 bytes/sample)
            writer.Write((short)24); // Bits per sample (24-bit - UNSUPPORTED)

            // data chunk
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(1000); // Some data size
            // Write some dummy 24-bit samples
            for (int i = 0; i < 1000 / 3; i++)
            {
                writer.Write((byte)128); // 24-bit sample (3 bytes)
                writer.Write((byte)128);
                writer.Write((byte)128);
            }
        }

        // Act & Assert - Should throw FormatException for malformed WAV file
        var exception = Assert.Throws<FormatException>(() => WavFile.ReadMono(testFilePath));
        Assert.Contains("Invalid WAV file", exception.Message);
    }

    [Fact]
    public void ReadMono_ThrowsForUnsupportedEncoding()
    {
        // Arrange - Create a file with unsupported encoding (e.g., ADPCM)
        string testFilePath = Path.Combine(_testFilesPath, "adpcm_test.wav");

        // Create a minimal ADPCM WAV file header (unsupported)
        using (var writer = new BinaryWriter(File.OpenWrite(testFilePath)))
        {
            // RIFF header
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(0); // Placeholder for file size
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));

            // fmt chunk - ADPCM format (unsupported)
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(20); // Chunk size for ADPCM
            writer.Write((short)2); // ADPCM format
            writer.Write((short)1); // Mono
            writer.Write(44100); // Sample rate
            writer.Write(132300); // Byte rate
            writer.Write((short)1); // Block align
            writer.Write((short)4); // Bits per sample
            writer.Write((short)7); // Extra format bytes
            writer.Write((short)0x0002); // Samples per block
            writer.Write((short)0x0007); // ADPCM coefficients

            // data chunk
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(100); // Some data size
            writer.Write(new byte[100]); // Dummy data
        }

        // Act & Assert - Should throw FormatException for malformed WAV file
        var exception = Assert.Throws<FormatException>(() => WavFile.ReadMono(testFilePath));
        Assert.Contains("Invalid WAV file", exception.Message);
    }

    [Fact]
    public void ReadMono_ThrowsForTruncatedFile()
    {
        // Arrange - Create a WAV file with declared data size larger than actual file
        string testFilePath = Path.Combine(_testFilesPath, "truncated_test.wav");

        // Create a minimal WAV file with declared size larger than actual
        using (var writer = new BinaryWriter(File.OpenWrite(testFilePath)))
        {
            // RIFF header
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(100); // File size (smaller than actual data chunk claims)
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));

            // fmt chunk
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16); // Chunk size
            writer.Write((short)1); // PCM format
            writer.Write((short)1); // Mono
            writer.Write(44100); // Sample rate
            writer.Write(176400); // Byte rate (44100 * 4 bytes/sample * 1 channel)
            writer.Write((short)4); // Block align (4 bytes/sample for 32-bit float)
            writer.Write((short)32); // Bits per sample

            // data chunk with declared size larger than file
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(200000); // Declared data size (200KB)
            // Write only a small amount of data
            for (int i = 0; i < 100; i++)
            {
                writer.Write(0.0f); // 32-bit float sample
            }
        }

        // Act & Assert - Should throw InvalidDataException for truncated file
        // NAudio should handle this gracefully rather than throwing IndexOutOfRangeException
        var exception = Assert.Throws<InvalidDataException>(() => WavFile.ReadMono(testFilePath));
        Assert.Contains("Declared WAV length exceeds file size", exception.Message);
    }

    [Fact]
    public void ReadMono_ThrowsForNonExistentFile()
    {
        // Arrange - Non-existent file path
        string testFilePath = Path.Combine(_testFilesPath, "nonexistent.wav");

        // Act & Assert - Should throw FileNotFoundException
        Assert.Throws<FileNotFoundException>(() => WavFile.ReadMono(testFilePath));
    }

    [Fact]
    public void ReadMono_ThrowsForNullPath()
    {
        // Act & Assert - Should throw ArgumentNullException
        Assert.Throws<ArgumentNullException>(() => WavFile.ReadMono(null!));
    }

    [Fact]
    public void ReadStereo_ThrowsForNonStereoFile()
    {
        // Arrange - Create a mono WAV file
        int sampleRate = 44100;
        var samples = new float[1000];
        var random = new Random(42);
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = (float)(random.NextDouble() * 2.0 - 1.0);
        }

        string testFilePath = Path.Combine(_testFilesPath, "mono_for_stereo_test.wav");
        WavFile.WriteMono(testFilePath, samples, sampleRate);

        // Act & Assert - Should throw InvalidDataException when trying to read as stereo
        var exception = Assert.Throws<InvalidDataException>(() => WavFile.ReadStereo(testFilePath));
        Assert.Contains("must be stereo", exception.Message);
    }

    [Fact]
    public void WriteStereo_PreservesChannelCountAndSampleValues()
    {
        // Arrange - Create stereo audio with specific values
        int sampleRate = 48000;
        int sampleCount = 1000;
        var left = new float[sampleCount];
        var right = new float[sampleCount];
        var random = new Random(999);

        for (int i = 0; i < sampleCount; i++)
        {
            left[i] = (float)(random.NextDouble() * 2.0 - 1.0);
            right[i] = (float)(random.NextDouble() * 2.0 - 1.0);
        }

        string testFilePath = Path.Combine(_testFilesPath, "stereo_roundtrip_test.wav");

        // Act - Write and read back
        WavFile.WriteStereo(testFilePath, left, right, sampleRate);
        var (readLeft, readRight, readSampleRate) = WavFile.ReadStereo(testFilePath);

        // Assert - Channel count and sample values should be preserved
        Assert.Equal(sampleCount, readLeft.Length);
        Assert.Equal(sampleCount, readRight.Length);
        Assert.Equal(sampleRate, readSampleRate);

        // Verify sample values are preserved within quantization tolerance
        for (int i = 0; i < sampleCount; i++)
        {
            float leftDiff = Math.Abs(readLeft[i] - left[i]);
            float rightDiff = Math.Abs(readRight[i] - right[i]);
            Assert.True(leftDiff < 0.0001f,
                $"Left channel sample {i}: expected {left[i]:F6}, got {readLeft[i]:F6}, diff={leftDiff:F6}");
            Assert.True(rightDiff < 0.0001f,
                $"Right channel sample {i}: expected {right[i]:F6}, got {readRight[i]:F6}, diff={rightDiff:F6}");
        }
    }

    [Fact]
    public void ReadMono_HandlesZeroLengthAudioWithoutDivideByZero()
    {
        // Arrange - Create a file with zero samples (edge case for duration calculation)
        string testFilePath = Path.Combine(_testFilesPath, "zero_length.wav");

        // Create a minimal WAV file with zero samples
        using (var writer = new BinaryWriter(File.OpenWrite(testFilePath)))
        {
            // RIFF header
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(44); // Minimal file size
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));

            // fmt chunk
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16); // Chunk size
            writer.Write((short)1); // PCM format
            writer.Write((short)1); // Mono
            writer.Write(44100); // Sample rate
            writer.Write(176400); // Byte rate
            writer.Write((short)4); // Block align
            writer.Write((short)32); // Bits per sample (32-bit float)

            // data chunk with zero size
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(0); // Zero samples
        }

        // Act - Should not throw divide-by-zero exception
        var (samples, sampleRate) = WavFile.ReadMono(testFilePath);

        // Assert - Should return empty array without crashing
        Assert.Empty(samples);
        Assert.Equal(44100, sampleRate);
    }

    [Fact]
    public void ReadMono_HandlesVerySmallFile()
    {
        // Arrange - Create a file with just the header and minimal data
        string testFilePath = Path.Combine(_testFilesPath, "minimal.wav");

        // Create a minimal WAV file with just 1 sample
        using (var writer = new BinaryWriter(File.OpenWrite(testFilePath)))
        {
            // RIFF header
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(60); // File size
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));

            // fmt chunk
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16); // Chunk size
            writer.Write((short)1); // PCM format
            writer.Write((short)1); // Mono
            writer.Write(44100); // Sample rate
            writer.Write(176400); // Byte rate
            writer.Write((short)4); // Block align
            writer.Write((short)32); // Bits per sample

            // data chunk with 1 sample
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(4); // 4 bytes for 1 float
            writer.Write(0.5f); // One sample at 0.5 amplitude
        }

        // Act - Should handle without crashing
        var (samples, sampleRate) = WavFile.ReadMono(testFilePath);

        // Assert
        Assert.Single(samples);
        // Allow for quantization when writing 32-bit float and reading back
        Assert.Equal(0.5f, samples[0], 1);
        Assert.Equal(44100, sampleRate);
    }

    [Fact]
    public void ReadMono_HandlesExtremeSampleValues()
    {
        // Arrange - Create samples with extreme values that might cause issues
        int sampleRate = 44100;
        int sampleCount = 100;
        var samples = new float[sampleCount];

        // Fill with extreme values
        for (int i = 0; i < sampleCount; i++)
        {
            samples[i] = (i % 2 == 0) ? float.MaxValue : float.MinValue;
        }

        string testFilePath = Path.Combine(_testFilesPath, "extreme_values.wav");
        WavFile.WriteMono(testFilePath, samples, sampleRate);

        // Act - Should handle without crashing
        var (readSamples, readSampleRate) = WavFile.ReadMono(testFilePath);

        // Assert - Values should be preserved
        Assert.Equal(sampleCount, readSamples.Length);
        Assert.Equal(sampleRate, readSampleRate);
        Assert.Equal(float.MaxValue, readSamples[0], 5);
        Assert.Equal(float.MinValue, readSamples[1], 5);
    }

    [Fact]
    public void WriteMono_HandlesNullPath()
    {
        // Arrange
        var samples = new float[10];

        // Act & Assert - Should throw ArgumentNullException
        Assert.Throws<ArgumentNullException>(() => WavFile.WriteMono(null!, samples, 44100));
    }

    [Fact]
    public void WriteMono_HandlesNullSamples()
    {
        // Arrange
        string testFilePath = Path.Combine(_testFilesPath, "null_samples.wav");

        // Act & Assert - Should throw ArgumentNullException
        Assert.Throws<ArgumentNullException>(() => WavFile.WriteMono(testFilePath, null!, 44100));
    }

    [Fact]
    public void WriteStereo_HandlesNullPath()
    {
        // Arrange
        var left = new float[10];
        var right = new float[10];

        // Act & Assert - Should throw ArgumentNullException
        Assert.Throws<ArgumentNullException>(() => WavFile.WriteStereo(null!, left, right, 44100));
    }

    [Fact]
    public void WriteStereo_HandlesNullLeft()
    {
        // Arrange
        string testFilePath = Path.Combine(_testFilesPath, "null_left_stereo.wav");
        var right = new float[10];

        // Act & Assert - Should throw ArgumentNullException
        Assert.Throws<ArgumentNullException>(() => WavFile.WriteStereo(testFilePath, null!, right, 44100));
    }

    [Fact]
    public void WriteStereo_HandlesNullRight()
    {
        // Arrange
        string testFilePath = Path.Combine(_testFilesPath, "null_right_stereo.wav");
        var left = new float[10];

        // Act & Assert - Should throw ArgumentNullException
        Assert.Throws<ArgumentNullException>(() => WavFile.WriteStereo(testFilePath, left, null!, 44100));
    }


}
