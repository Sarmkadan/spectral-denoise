namespace SpectralDenoise;

/// <summary>
/// Validation messages for <see cref="NoiseSegmentDetector"/>.
/// </summary>
internal static class NoiseSegmentDetectorConstants
{
    public const string ErrorSecondsPositive = "Seconds must be a finite number greater than zero.";
    public const string ErrorSignalEmpty = "Signal must contain at least one sample.";
}
