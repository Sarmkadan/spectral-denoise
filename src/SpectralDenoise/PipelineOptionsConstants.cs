namespace SpectralDenoise;

/// <summary>
/// Default values and validation messages for <see cref="PipelineOptions"/>.
/// </summary>
internal static class PipelineOptionsConstants
{
    public const int DefaultSampleRate = 44_100;
    public const int DefaultFrameSize = 1024;
    public const int DefaultHop = 256;
    public const double DefaultOverSubtractionFactor = 2.0;
    public const double DefaultSpectralFloor = 0.02;

    public const string SampleRateMessage = "SampleRate must be greater than zero (was {0}).";
    public const string FrameSizeMessage = "FrameSize must be a power of two (was {0}).";
    public const string HopMessage = "Hop must be greater than zero and no larger than FrameSize (was {0}, FrameSize {1}).";
    public const string OverSubtractionFactorMessage = "OverSubtractionFactor must be a finite number >= 1 (was {0}).";
    public const string SpectralFloorMessage = "SpectralFloor must be in the range [0, 1) (was {0}).";
    public const string InvalidOptionsHeader = "Invalid pipeline options ({0} problem(s)):";
}
