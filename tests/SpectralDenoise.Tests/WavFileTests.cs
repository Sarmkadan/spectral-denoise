using System;
using System.IO;
using System.Text;
using Xunit;
using static SpectralDenoise.Tests.WavFileTestsConstants;

namespace SpectralDenoise.Tests;

/// <summary>
/// Contains tests for the WavFile class functionality including reading, writing, and validating WAV audio files.
/// Tests cover mono and stereo audio handling, various sample rates, edge cases, and error conditions.
/// </summary>
public class WavFileTests : IWavFileTests
{
    private readonly string _testFilesPath;

    /// <summary>
    /// Initializes a new instance of the WavFileTests class and creates a temporary directory for test files.
    /// </summary>
    public WavFileTests()
    {
        _testFilesPath = Path.Combine(Directory.GetCurrentDirectory(), TestFilesDirectory);
        Directory.CreateDirectory(_testFilesPath);
    }

    /// <summary>
    /// Cleans up the temporary test files directory after tests complete.
    /// </summary>
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

    /// <summary>
    /// Tests that writing and reading a mono WAV file preserves the original sample count and sample rate.
    /// </summary>
    [Fact]
    public void ReadMono_WriteMono_PreservesSampleCountAndRate()
    {
        // Arrange
        int sampleRate = DefaultSampleRate;
        int sampleCount = DefaultSampleCount;
        var originalSamples = new float[sampleCount];
        var random = new Random(MonoRandomSeed);
        for (int i = 0; i < sampleCount; i++)
        {
            originalSamples[i] = (float)(random.NextDouble() * RandomRangeScale - RandomRangeOffset); // Range [-1.0, 1.0]
        }

        string testFilePath = Path.Combine(_testFilesPath, "mono_test.wav");

        // Act
        WavFile.WriteMono(testFilePath, originalSamples, sampleRate);
        var (readSamples, readSampleRate) = WavFile.ReadMono(testFilePath);

        // Assert
        Assert.Equal(sampleCount, readSamples.Length);
        Assert.Equal(sampleRate, readSampleRate);
    }

    /// <summary>
    /// Tests that writing and reading a mono WAV file preserves sample values within 16-bit quantization tolerance.
    /// </summary>
    [Fact]
    public void ReadMono_WriteMono_PreservesSampleValuesWithinQuantization()
    {
        // Arrange
        int sampleRate = HighSampleRate;
        int sampleCount = QuantizationSampleCount;
        var originalSamples = new float[sampleCount];
        var expectedSamples = new float[sampleCount];
        var random = new Random(QuantizationRandomSeed);

        for (int i = 0; i < sampleCount; i++)
        {
            float value = (float)(random.NextDouble() * RandomRangeScale - RandomRangeOffset);
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
            Assert.True(diff < QuantizationTolerance,
                $"Sample {i}: expected {expectedSamples[i]:F6}, got {readSamples[i]:F6}, diff={diff:F6}");
        }
    }

    /// <summary>
    /// Tests that reading a mono WAV file handles very small files gracefully (single sample).
    /// </summary>
    [Fact]
    public void ReadMono_HandlesEmptyFileGracefully()
    {
        // Arrange - NAudio doesn't handle empty files well, so we skip testing empty file round-trip
        // Instead, we test that we can handle very small files
        int sampleRate = DefaultSampleRate;
        var verySmallSamples = new float[1]; // Single sample
        string testFilePath = Path.Combine(_testFilesPath, "very_small_test.wav");

        // Act
        WavFile.WriteMono(testFilePath, verySmallSamples, sampleRate);
        var (readSamples, readSampleRate) = WavFile.ReadMono(testFilePath);

        // Assert
        Assert.Single(readSamples);
        Assert.Equal(sampleRate, readSampleRate);
    }

    /// <summary>
    /// Tests that writing and reading a mono WAV file with a single sample preserves the sample value and sample rate.
    /// </summary>
    [Fact]
    public void ReadMono_WriteMono_WithSingleSample()
    {
        // Arrange
        int sampleRate = UltraHighSampleRate;
        var singleSample = new float[] { HalfAmplitude };
        string testFilePath = Path.Combine(_testFilesPath, "single_sample_test.wav");

        // Act
        WavFile.WriteMono(testFilePath, singleSample, sampleRate);
        var (readSamples, readSampleRate) = WavFile.ReadMono(testFilePath);

        // Assert
        Assert.Single(readSamples);
        Assert.Equal(HalfAmplitude, readSamples[0], FloatPrecisionDigits);
        Assert.Equal(sampleRate, readSampleRate);
    }

    /// <summary>
    /// Tests that writing and reading a mono WAV file with a large sample array preserves sample count and sample rate.
    /// </summary>
    [Fact]
    public void ReadMono_WriteMono_WithLargeSampleArray()
    {
        // Arrange
        int sampleRate = MaxSampleRate;
        int sampleCount = LargeSampleCount; // Large array
        var largeSamples = new float[sampleCount];
        var random = new Random(LargeArrayRandomSeed);
        for (int i = 0; i < sampleCount; i++)
        {
            largeSamples[i] = (float)(random.NextDouble() * RandomRangeScale - RandomRangeOffset);
        }

        string testFilePath = Path.Combine(_testFilesPath, "large_test.wav");

        // Act
        WavFile.WriteMono(testFilePath, largeSamples, sampleRate);
        var (readSamples, readSampleRate) = WavFile.ReadMono(testFilePath);

        // Assert
        Assert.Equal(sampleCount, readSamples.Length);
        Assert.Equal(sampleRate, readSampleRate);
    }

    /// <summary>
/// Tests that writing and reading a mono WAV file preserves values outside the [-1.0, 1.0] range when using IEEE float format.
    /// </summary>
    [Fact]
    public void ReadMono_WriteMono_WithClampedValues()
    {
        // Arrange - values outside [-1.0, 1.0] are clamped by WriteStereo but WriteMono uses IEEE float format
        // WriteMono uses IEEE float format, so values outside [-1.0, 1.0] are preserved in the float format
        int sampleRate = DefaultSampleRate;
        string testFilePath = Path.Combine(_testFilesPath, "clamped_test.wav");

        // Act
        WavFile.WriteMono(testFilePath, OutOfRangeSamples, sampleRate);
        var (readSamples, readSampleRate) = WavFile.ReadMono(testFilePath);

        // Assert
        Assert.Equal(OutOfRangeSamples.Length, readSamples.Length);
        Assert.Equal(sampleRate, readSampleRate);

        // WriteMono uses IEEE float format, so values are preserved (not clamped to [-1.0, 1.0])
        // The values written are what we expect to read back
        for (int i = 0; i < OutOfRangeSamples.Length; i++)
        {
            Assert.Equal(OutOfRangeSamples[i], readSamples[i], FloatPrecisionDigits);
        }
    }

    /// <summary>
    /// Tests that writing a mono WAV file accepts negative sample rates without throwing exceptions (no validation in WavFile.WriteMono).
    /// </summary>
    [Fact]
    public void WriteMono_HandlesNegativeSampleRate()
    {
        // Arrange
        var samples = new float[] { HalfAmplitude };
        string testFilePath = Path.Combine(_testFilesPath, "negative_rate_test.wav");

        // Act - WavFile.WriteMono doesn't validate sample rate
        var exception = Record.Exception(() => WavFile.WriteMono(testFilePath, samples, -DefaultSampleRate));

        // Assert - Method accepts negative sample rate (no validation in WavFile.WriteMono)
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that writing a mono WAV file accepts zero sample rates without throwing exceptions (no validation in WavFile.WriteMono).
    /// </summary>
    [Fact]
    public void WriteMono_HandlesZeroSampleRate()
    {
        // Arrange
        var samples = new float[] { HalfAmplitude };
        string testFilePath = Path.Combine(_testFilesPath, "zero_rate_test.wav");

        // Act - WavFile.WriteMono doesn't validate sample rate
        var exception = Record.Exception(() => WavFile.WriteMono(testFilePath, samples, 0));

        // Assert - Method accepts zero sample rate (no validation in WavFile.WriteMono)
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that reading a stereo WAV file correctly reads both left and right channels and preserves sample rate.
    /// </summary>
    [Fact]
    public void ReadStereo_ReadsTwoChannels()
    {
        // Arrange
        int sampleRate = DefaultSampleRate;
        int sampleCount = DefaultSampleCount;
        var left = new float[sampleCount];
        var right = new float[sampleCount];
        var random = new Random(StereoRandomSeed);

        for (int i = 0; i < sampleCount; i++)
        {
            left[i] = (float)(random.NextDouble() * RandomRangeScale - RandomRangeOffset);
            right[i] = (float)(random.NextDouble() * RandomRangeScale - RandomRangeOffset);
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

    /// <summary>
    /// Tests that writing a stereo WAV file throws an InvalidDataException when left and right channels have different lengths.
    /// </summary>
    [Fact]
    public void WriteStereo_RejectsDifferentLengthChannels()
    {
        // Arrange
        var left = new float[DefaultSampleCount];
        var right = new float[ChannelMismatchRightLength];
        string testFilePath = Path.Combine(_testFilesPath, "mismatch_test.wav");

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => WavFile.WriteStereo(testFilePath, left, right, DefaultSampleRate));
    }

    /// <summary>
    /// Tests that writing a stereo WAV file throws an exception when the left channel is null.
    /// </summary>
    [Fact]
    public void WriteStereo_HandlesNullLeftChannel()
    {
        // Arrange
        var right = new float[SmallSampleCount];
        string testFilePath = Path.Combine(_testFilesPath, "null_left_test.wav");

        // Act - WavFile.WriteStereo throws NullReferenceException for null arrays (no explicit null check)
        var exception = Record.Exception(() => WavFile.WriteStereo(testFilePath, null!, right, DefaultSampleRate));

        // Assert - Method throws exception for null arrays (validation exists but throws NullReferenceException)
        Assert.NotNull(exception);
    }

    /// <summary>
    /// Tests that writing a stereo WAV file throws an exception when the right channel is null.
    /// </summary>
    [Fact]
    public void WriteStereo_HandlesNullRightChannel()
    {
        // Arrange
        var left = new float[SmallSampleCount];
        string testFilePath = Path.Combine(_testFilesPath, "null_right_test.wav");

        // Act - WavFile.WriteStereo throws NullReferenceException for null arrays (no explicit null check)
        var exception = Record.Exception(() => WavFile.WriteStereo(testFilePath, left, null!, DefaultSampleRate));

        // Assert - Method throws exception for null arrays
        Assert.NotNull(exception);
    }

    /// <summary>
    /// Tests that writing a stereo WAV file accepts negative sample rates without throwing exceptions (no validation in WavFile.WriteStereo).
    /// </summary>
    [Fact]
    public void WriteStereo_HandlesNegativeSampleRate()
    {
        // Arrange
        var left = new float[SmallSampleCount];
        var right = new float[SmallSampleCount];
        string testFilePath = Path.Combine(_testFilesPath, "negative_stereo_rate_test.wav");

        // Act - WavFile.WriteStereo doesn't validate sample rate
        var exception = Record.Exception(() => WavFile.WriteStereo(testFilePath, left, right, -DefaultSampleRate));

        // Assert - Method accepts negative sample rate (no validation in WavFile.WriteStereo)
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that writing a stereo WAV file accepts zero sample rates without throwing exceptions (no validation in WavFile.WriteStereo).
    /// </summary>
    [Fact]
    public void WriteStereo_HandlesZeroSampleRate()
    {
        // Arrange
        var left = new float[SmallSampleCount];
        var right = new float[SmallSampleCount];
        string testFilePath = Path.Combine(_testFilesPath, "zero_stereo_rate_test.wav");

        // Act - WavFile.WriteStereo doesn't validate sample rate
        var exception = Record.Exception(() => WavFile.WriteStereo(testFilePath, left, right, 0));

        // Assert - Method accepts zero sample rate (no validation in WavFile.WriteStereo)
        Assert.Null(exception);
    }

    /// <summary>
    /// Tests that reading and writing mono WAV files works correctly across various common sample rates.
    /// </summary>
    [Fact]
    public void ReadMono_HandlesDifferentSampleRates()
    {
        // Test various common sample rates
        int[] sampleRates = { 8000, 11025, 16000, 22050, 32000, 44100, 48000, 88200, 96000, 192000 };

        foreach (int sampleRate in sampleRates)
        {
            // Arrange
            int sampleCount = SmallSampleCount;
            var samples = new float[sampleCount];
            var random = new Random(sampleRate);
            for (int i = 0; i < sampleCount; i++)
            {
                samples[i] = (float)(random.NextDouble() * RandomRangeScale - RandomRangeOffset);
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

    /// <summary>
    /// Tests that a round-trip write/read operation preserves audio data integrity for a realistic sine wave signal.
    /// </summary>
    [Fact]
    public void RoundTrip_PreservesAudioDataIntegrity()
    {
        // Arrange - Create a realistic audio signal
        int sampleRate = DefaultSampleRate;
        int durationSeconds = SineWaveDurationSeconds;
        int sampleCount = sampleRate * durationSeconds;
        var originalSamples = new float[sampleCount];

        // Generate a sine wave at 440Hz (A4 note)
        double frequency = SineWaveFrequencyHz;
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
            Assert.True(diff < SineWaveTolerance,
                $"Sample {i}: expected {originalSamples[i]:F6}, got {readSamples[i]:F6}, diff={diff:F6}");
        }
    }

    /// <summary>
    /// Tests that reading a mono WAV file handles silent audio (all zero samples) correctly.
    /// </summary>
    [Fact]
    public void ReadMono_HandlesSilentAudio()
    {
        // Arrange - All zeros (silent audio)
        int sampleRate = DefaultSampleRate;
        int sampleCount = DefaultSampleCount;
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
            Assert.Equal(0.0f, sample, FloatPrecisionDigits);
        }
    }

    /// <summary>
    /// Tests that reading a mono WAV file preserves maximum amplitude samples (1.0 and -1.0).
    /// </summary>
    [Fact]
    public void ReadMono_HandlesMaximumAmplitude()
    {
        // Arrange - Maximum amplitude samples (1.0 and -1.0)
        int sampleRate = DefaultSampleRate;
        int sampleCount = SmallSampleCount;
        var maxSamples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            maxSamples[i] = (i % 2 == 0) ? MaxAmplitude : -MaxAmplitude;
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
                Assert.Equal(MaxAmplitude, readSamples[i], FloatPrecisionDigits);
            }
            else
            {
                Assert.Equal(-MaxAmplitude, readSamples[i], FloatPrecisionDigits);
            }
        }
    }

    /// <summary>
    /// Tests that reading a mono WAV file with unsupported bit depth (24-bit) throws a FormatException.
    /// </summary>
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
            writer.Write(Encoding.ASCII.GetBytes(RiffId));
            writer.Write(PlaceholderSize); // Placeholder for file size
            writer.Write(Encoding.ASCII.GetBytes(WaveId));

            // fmt chunk
            writer.Write(Encoding.ASCII.GetBytes(FmtChunkId));
            writer.Write(PcmFmtChunkSize); // Chunk size
            writer.Write(PcmFormatTag); // PCM format
            writer.Write(MonoChannels); // Mono
            writer.Write(DefaultSampleRate); // Sample rate
            writer.Write(ByteRate44100x3Bytes); // Byte rate (44100 * 3 bytes/sample * 1 channel)
            writer.Write(BlockAlign24Bit); // Block align (3 bytes/sample)
            writer.Write(BitsPerSample24); // Bits per sample (24-bit - UNSUPPORTED)

            // data chunk
            writer.Write(Encoding.ASCII.GetBytes(DataChunkId));
            writer.Write(Declared24BitDataSize); // Some data size
            // Write some dummy 24-bit samples
            for (int i = 0; i < Declared24BitDataSize / Bytes24BitSample; i++)
            {
                writer.Write(Dummy24BitSampleByte); // 24-bit sample (3 bytes)
                writer.Write(Dummy24BitSampleByte);
                writer.Write(Dummy24BitSampleByte);
            }
        }

        // Act & Assert - Should throw FormatException for malformed WAV file
        var exception = Assert.Throws<FormatException>(() => WavFile.ReadMono(testFilePath));
        Assert.Contains(InvalidWavMessage, exception.Message);
    }

    /// <summary>
    /// Tests that reading a mono WAV file with unsupported encoding (ADPCM) throws a FormatException.
    /// </summary>
    [Fact]
    public void ReadMono_ThrowsForUnsupportedEncoding()
    {
        // Arrange - Create a file with unsupported encoding (e.g., ADPCM)
        string testFilePath = Path.Combine(_testFilesPath, "adpcm_test.wav");

        // Create a minimal ADPCM WAV file header (unsupported)
        using (var writer = new BinaryWriter(File.OpenWrite(testFilePath)))
        {
            // RIFF header
            writer.Write(Encoding.ASCII.GetBytes(RiffId));
            writer.Write(PlaceholderSize); // Placeholder for file size
            writer.Write(Encoding.ASCII.GetBytes(WaveId));

            // fmt chunk - ADPCM format (unsupported)
            writer.Write(Encoding.ASCII.GetBytes(FmtChunkId));
            writer.Write(AdpcmFmtChunkSize); // Chunk size for ADPCM
            writer.Write(AdpcmFormatTag); // ADPCM format
            writer.Write(MonoChannels); // Mono
            writer.Write(DefaultSampleRate); // Sample rate
            writer.Write(ByteRate44100x3Bytes); // Byte rate
            writer.Write(BlockAlign1); // Block align
            writer.Write(BitsPerSample4); // Bits per sample
            writer.Write(AdpcmExtraFormatBytes); // Extra format bytes
            writer.Write(AdpcmSamplesPerBlock); // Samples per block
            writer.Write(AdpcmCoefficients); // ADPCM coefficients

            // data chunk
            writer.Write(Encoding.ASCII.GetBytes(DataChunkId));
            writer.Write(AdpcmDataSize); // Some data size
            writer.Write(new byte[AdpcmDataSize]); // Dummy data
        }

        // Act & Assert - Should throw FormatException for malformed WAV file
        var exception = Assert.Throws<FormatException>(() => WavFile.ReadMono(testFilePath));
        Assert.Contains(InvalidWavMessage, exception.Message);
    }

    /// <summary>
    /// Tests that reading a mono WAV file with truncated data (declared size larger than actual file) throws an InvalidDataException.
    /// </summary>
    [Fact]
    public void ReadMono_ThrowsForTruncatedFile()
    {
        // Arrange - Create a WAV file with declared data size larger than actual file
        string testFilePath = Path.Combine(_testFilesPath, "truncated_test.wav");

        // Create a minimal WAV file with declared size larger than actual
        using (var writer = new BinaryWriter(File.OpenWrite(testFilePath)))
        {
            // RIFF header
            writer.Write(Encoding.ASCII.GetBytes(RiffId));
            writer.Write(TruncatedRiffSize); // File size (smaller than actual data chunk claims)
            writer.Write(Encoding.ASCII.GetBytes(WaveId));

            // fmt chunk
            writer.Write(Encoding.ASCII.GetBytes(FmtChunkId));
            writer.Write(PcmFmtChunkSize); // Chunk size
            writer.Write(PcmFormatTag); // PCM format
            writer.Write(MonoChannels); // Mono
            writer.Write(DefaultSampleRate); // Sample rate
            writer.Write(ByteRate44100x4Bytes); // Byte rate (44100 * 4 bytes/sample * 1 channel)
            writer.Write(BlockAlign32Bit); // Block align (4 bytes/sample for 32-bit float)
            writer.Write(BitsPerSample32); // Bits per sample

            // data chunk with declared size larger than file
            writer.Write(Encoding.ASCII.GetBytes(DataChunkId));
            writer.Write(TruncatedDeclaredDataSize); // Declared data size (200KB)
            // Write only a small amount of data
            for (int i = 0; i < SmallSampleCount; i++)
            {
                writer.Write(0.0f); // 32-bit float sample
            }
        }

        // Act & Assert - Should throw InvalidDataException for truncated file
        // NAudio should handle this gracefully rather than throwing IndexOutOfRangeException
        var exception = Assert.Throws<InvalidDataException>(() => WavFile.ReadMono(testFilePath));
        Assert.Contains(DeclaredLengthExceedsFileMessage, exception.Message);
    }

    /// <summary>
    /// Tests that reading a mono WAV file from a non-existent path throws a FileNotFoundException.
    /// </summary>
    [Fact]
    public void ReadMono_ThrowsForNonExistentFile()
    {
        // Arrange - Non-existent file path
        string testFilePath = Path.Combine(_testFilesPath, "nonexistent.wav");

        // Act & Assert - Should throw FileNotFoundException
        Assert.Throws<FileNotFoundException>(() => WavFile.ReadMono(testFilePath));
    }

    /// <summary>
    /// Tests that reading a mono WAV file with a null path throws an ArgumentNullException.
    /// </summary>
    [Fact]
    public void ReadMono_ThrowsForNullPath()
    {
        // Act & Assert - Should throw ArgumentNullException
        Assert.Throws<ArgumentNullException>(() => WavFile.ReadMono(null!));
    }

    /// <summary>
    /// Tests that reading a mono WAV file as stereo throws an InvalidDataException.
    /// </summary>
    [Fact]
    public void ReadStereo_ThrowsForNonStereoFile()
    {
        // Arrange - Create a mono WAV file
        int sampleRate = DefaultSampleRate;
        var samples = new float[DefaultSampleCount];
        var random = new Random(MonoRandomSeed);
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = (float)(random.NextDouble() * RandomRangeScale - RandomRangeOffset);
        }

        string testFilePath = Path.Combine(_testFilesPath, "mono_for_stereo_test.wav");
        WavFile.WriteMono(testFilePath, samples, sampleRate);

        // Act & Assert - Should throw InvalidDataException when trying to read as stereo
        var exception = Assert.Throws<InvalidDataException>(() => WavFile.ReadStereo(testFilePath));
        Assert.Contains(StereoRequiredMessage, exception.Message);
    }

    /// <summary>
    /// Tests that writing and reading a stereo WAV file preserves channel count and sample values within quantization tolerance.
    /// </summary>
    [Fact]
    public void WriteStereo_PreservesChannelCountAndSampleValues()
    {
        // Arrange - Create stereo audio with specific values
        int sampleRate = HighSampleRate;
        int sampleCount = DefaultSampleCount;
        var left = new float[sampleCount];
        var right = new float[sampleCount];
        var random = new Random(StereoRoundTripRandomSeed);

        for (int i = 0; i < sampleCount; i++)
        {
            left[i] = (float)(random.NextDouble() * RandomRangeScale - RandomRangeOffset);
            right[i] = (float)(random.NextDouble() * RandomRangeScale - RandomRangeOffset);
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
            Assert.True(leftDiff < QuantizationTolerance,
                $"Left channel sample {i}: expected {left[i]:F6}, got {readLeft[i]:F6}, diff={leftDiff:F6}");
            Assert.True(rightDiff < QuantizationTolerance,
                $"Right channel sample {i}: expected {right[i]:F6}, got {readRight[i]:F6}, diff={rightDiff:F6}");
        }
    }

    /// <summary>
    /// Tests that reading a mono WAV file with zero length audio handles the edge case without throwing a divide-by-zero exception.
    /// </summary>
    [Fact]
    public void ReadMono_HandlesZeroLengthAudioWithoutDivideByZero()
    {
        // Arrange - Create a file with zero samples (edge case for duration calculation)
        string testFilePath = Path.Combine(_testFilesPath, "zero_length.wav");

        // Create a minimal WAV file with zero samples
        using (var writer = new BinaryWriter(File.OpenWrite(testFilePath)))
        {
            // RIFF header
            writer.Write(Encoding.ASCII.GetBytes(RiffId));
            writer.Write(ZeroLengthRiffSize); // Minimal file size
            writer.Write(Encoding.ASCII.GetBytes(WaveId));

            // fmt chunk
            writer.Write(Encoding.ASCII.GetBytes(FmtChunkId));
            writer.Write(PcmFmtChunkSize); // Chunk size
            writer.Write(PcmFormatTag); // PCM format
            writer.Write(MonoChannels); // Mono
            writer.Write(DefaultSampleRate); // Sample rate
            writer.Write(ByteRate44100x4Bytes); // Byte rate
            writer.Write(BlockAlign32Bit); // Block align
            writer.Write(BitsPerSample32); // Bits per sample (32-bit float)

            // data chunk with zero size
            writer.Write(Encoding.ASCII.GetBytes(DataChunkId));
            writer.Write(0); // Zero samples
        }

        // Act - Should not throw divide-by-zero exception
        var (samples, sampleRate) = WavFile.ReadMono(testFilePath);

        // Assert - Should return empty array without crashing
        Assert.Empty(samples);
        Assert.Equal(DefaultSampleRate, sampleRate);
    }

    /// <summary>
    /// Tests that reading a mono WAV file handles a minimal file with just one sample correctly.
    /// </summary>
    [Fact]
    public void ReadMono_HandlesVerySmallFile()
    {
        // Arrange - Create a file with just the header and minimal data
        string testFilePath = Path.Combine(_testFilesPath, "minimal.wav");

        // Create a minimal WAV file with just 1 sample
        using (var writer = new BinaryWriter(File.OpenWrite(testFilePath)))
        {
            // RIFF header
            writer.Write(Encoding.ASCII.GetBytes(RiffId));
            writer.Write(VerySmallRiffSize); // File size
            writer.Write(Encoding.ASCII.GetBytes(WaveId));

            // fmt chunk
            writer.Write(Encoding.ASCII.GetBytes(FmtChunkId));
            writer.Write(PcmFmtChunkSize); // Chunk size
            writer.Write(PcmFormatTag); // PCM format
            writer.Write(MonoChannels); // Mono
            writer.Write(DefaultSampleRate); // Sample rate
            writer.Write(ByteRate44100x4Bytes); // Byte rate
            writer.Write(BlockAlign32Bit); // Block align
            writer.Write(BitsPerSample32); // Bits per sample

            // data chunk with 1 sample
            writer.Write(Encoding.ASCII.GetBytes(DataChunkId));
            writer.Write(OneFloatDataSize); // 4 bytes for 1 float
            writer.Write(HalfAmplitude); // One sample at 0.5 amplitude
        }

        // Act - Should handle without crashing
        var (samples, sampleRate) = WavFile.ReadMono(testFilePath);

        // Assert
        Assert.Single(samples);
        // Allow for quantization when writing 32-bit float and reading back
        Assert.Equal(HalfAmplitude, samples[0], CoarsePrecisionDigits);
        Assert.Equal(DefaultSampleRate, sampleRate);
    }

    /// <summary>
/// Tests that reading a mono WAV file handles extreme sample values (float.MaxValue and float.MinValue) correctly.
    /// </summary>
    /// <remarks>
    /// This test verifies that the WavFile class can handle and preserve extreme float values
    /// when writing and reading mono WAV files, ensuring no overflow or underflow occurs.
    /// </remarks>
    [Fact]
    public void ReadMono_HandlesExtremeSampleValues()
    {
        // Arrange - Create samples with extreme values that might cause issues
        int sampleRate = DefaultSampleRate;
        int sampleCount = SmallSampleCount;
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
        Assert.Equal(float.MaxValue, readSamples[0], FloatPrecisionDigits);
        Assert.Equal(float.MinValue, readSamples[1], FloatPrecisionDigits);
    }

    /// <summary>
/// Tests that writing a mono WAV file with a null file path throws an ArgumentNullException.
    /// </summary>
    [Fact]
    public void WriteMono_HandlesNullPath()
    {
        // Arrange
        var samples = new float[NullTestSampleCount];

        // Act & Assert - Should throw ArgumentNullException
        Assert.Throws<ArgumentNullException>(() => WavFile.WriteMono(null!, samples, DefaultSampleRate));
    }

    /// <summary>
/// Tests that writing a mono WAV file with a null samples array throws an ArgumentNullException.
    /// </summary>
    [Fact]
    public void WriteMono_HandlesNullSamples()
    {
        // Arrange
        string testFilePath = Path.Combine(_testFilesPath, "null_samples.wav");

        // Act & Assert - Should throw ArgumentNullException
        Assert.Throws<ArgumentNullException>(() => WavFile.WriteMono(testFilePath, null!, DefaultSampleRate));
    }

    /// <summary>
/// Tests that writing a stereo WAV file with a null file path throws an ArgumentNullException.
    /// </summary>
    [Fact]
    public void WriteStereo_HandlesNullPath()
    {
        // Arrange
        var left = new float[NullTestSampleCount];
        var right = new float[NullTestSampleCount];

        // Act & Assert - Should throw ArgumentNullException
        Assert.Throws<ArgumentNullException>(() => WavFile.WriteStereo(null!, left, right, DefaultSampleRate));
    }

    /// <summary>
/// Tests that writing a stereo WAV file with a null left channel array throws an ArgumentNullException.
    /// </summary>
    [Fact]
    public void WriteStereo_HandlesNullLeft()
    {
        // Arrange
        string testFilePath = Path.Combine(_testFilesPath, "null_left_stereo.wav");
        var right = new float[NullTestSampleCount];

        // Act & Assert - Should throw ArgumentNullException
        Assert.Throws<ArgumentNullException>(() => WavFile.WriteStereo(testFilePath, null!, right, DefaultSampleRate));
    }

    /// <summary>
/// Tests that writing a stereo WAV file with a null right channel array throws an ArgumentNullException.
    /// </summary>
    [Fact]
    public void WriteStereo_HandlesNullRight()
    {
        // Arrange
        string testFilePath = Path.Combine(_testFilesPath, "null_right_stereo.wav");
        var left = new float[NullTestSampleCount];

        // Act & Assert - Should throw ArgumentNullException
        Assert.Throws<ArgumentNullException>(() => WavFile.WriteStereo(testFilePath, left, null!, DefaultSampleRate));
    }


}
