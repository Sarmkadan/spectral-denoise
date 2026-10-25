namespace SpectralDenoise.Tests;

/// <summary>
/// Shared constants for <see cref="WavFileTests"/>: sample rates, sample counts, random seeds,
/// tolerances, and the raw RIFF/WAVE header values used when hand-crafting WAV files.
/// </summary>
internal static class WavFileTestsConstants
{
    // Directory (relative to the working directory) for temporary WAV files
    public const string TestFilesDirectory = "TestFiles";

    // Expected exception message fragments
    public const string InvalidWavMessage = "Invalid WAV file";
    public const string DeclaredLengthExceedsFileMessage = "Declared WAV length exceeds file size";
    public const string StereoRequiredMessage = "must be stereo";

    // Sample rates
    public const int DefaultSampleRate = 44100;
    public const int HighSampleRate = 48000;
    public const int UltraHighSampleRate = 96000;
    public const int MaxSampleRate = 192000;

    // Sample counts
    public const int DefaultSampleCount = 1000;
    public const int QuantizationSampleCount = 500;
    public const int SmallSampleCount = 100;
    public const int LargeSampleCount = 100000;
    public const int ChannelMismatchRightLength = 500;
    public const int NullTestSampleCount = 10;

    // Sine wave test signal
    public const int SineWaveDurationSeconds = 2;
    public const double SineWaveFrequencyHz = 440.0;

    // Random seeds for deterministic synthetic signals
    public const int MonoRandomSeed = 42;
    public const int QuantizationRandomSeed = 123;
    public const int LargeArrayRandomSeed = 456;
    public const int StereoRandomSeed = 789;
    public const int StereoRoundTripRandomSeed = 999;

    // NextDouble() * RandomRangeScale - RandomRangeOffset maps [0, 1) to [-1.0, 1.0)
    public const double RandomRangeScale = 2.0;
    public const double RandomRangeOffset = 1.0;

    // Amplitudes
    public const float MaxAmplitude = 1.0f;
    public const float HalfAmplitude = 0.5f;

    // Samples that exceed [-1.0, 1.0], preserved by the IEEE float writer
    public static readonly float[] OutOfRangeSamples = { -2.0f, -1.5f, -1.0f, 0.0f, 1.0f, 1.5f, 2.0f };

    // Tolerances
    public const float QuantizationTolerance = 0.0001f;
    public const float SineWaveTolerance = 0.001f;

    // Assert.Equal precision (decimal digits)
    public const int FloatPrecisionDigits = 5;
    public const int CoarsePrecisionDigits = 1;

    // RIFF chunk identifiers
    public const string RiffId = "RIFF";
    public const string WaveId = "WAVE";
    public const string FmtChunkId = "fmt ";
    public const string DataChunkId = "data";

    // WAV format tags and channel count
    public const short PcmFormatTag = 1;
    public const short AdpcmFormatTag = 2;
    public const short MonoChannels = 1;

    // fmt chunk sizes
    public const int PcmFmtChunkSize = 16;
    public const int AdpcmFmtChunkSize = 20;

    // Placeholder for header fields written before the real size is known
    public const int PlaceholderSize = 0;

    // Bit depths and block alignment
    public const short BitsPerSample32 = 32;
    public const short BitsPerSample24 = 24;
    public const short BitsPerSample4 = 4;
    public const short BlockAlign32Bit = 4;
    public const short BlockAlign24Bit = 3;
    public const short BlockAlign1 = 1;
    public const int Bytes24BitSample = 3;

    // Byte rates for 44100 Hz mono
    public const int ByteRate44100x3Bytes = 132300;
    public const int ByteRate44100x4Bytes = 176400;

    // ADPCM fmt extension fields
    public const short AdpcmExtraFormatBytes = 7;
    public const short AdpcmSamplesPerBlock = 0x0002;
    public const short AdpcmCoefficients = 0x0007;

    // Filler byte for the dummy 24-bit sample data
    public const byte Dummy24BitSampleByte = 128;

    // Declared sizes for hand-crafted WAV files
    public const int Declared24BitDataSize = 1000;
    public const int AdpcmDataSize = 100;
    public const int TruncatedRiffSize = 100;
    public const int TruncatedDeclaredDataSize = 200000;
    public const int ZeroLengthRiffSize = 44;
    public const int VerySmallRiffSize = 60;
    public const int OneFloatDataSize = 4;
}
