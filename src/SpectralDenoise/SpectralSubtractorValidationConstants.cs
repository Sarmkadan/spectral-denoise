namespace SpectralDenoise;

/// <summary>
/// Centralised constants for <see cref="SpectralSubtractorValidation"/> to avoid magic values scattered throughout the validation code.
/// </summary>
internal static class SpectralSubtractorValidationConstants
{
    // ------------------------------------------------------------------------
    // Message fragments
    // ------------------------------------------------------------------------
    public const string NotNanMessage = "must not be NaN.";
    public const string NotInfiniteMessage = "must not be infinite.";

    // ------------------------------------------------------------------------
    // Parameter names
    // ------------------------------------------------------------------------
    public const string DefaultNoiseProfileParamName = "noiseProfile";

    // ------------------------------------------------------------------------
    // Numeric limits
    // ------------------------------------------------------------------------
    public const double MinAlpha = 1.0;                                             // Lower bound for Alpha (over-subtraction factor); 1.0 = plain Boll
    public const double MinBeta = SpectralSubtractorConstants.MinSpectralFloor;     // Inclusive lower bound for Beta (spectral floor fraction)
    public const double MaxBeta = SpectralSubtractorConstants.MaxSpectralFloor;     // Inclusive upper bound for Beta (spectral floor fraction)
    public const int MinSampleRate = 8000;                                          // Lowest sample rate accepted by the pipeline, in Hz
    public const int MaxSampleRate = 192000;                                        // Highest sample rate accepted by the pipeline, in Hz
    public const int MinFrameSize = SpectralSubtractorConstants.MinFrameSize;       // Minimum frame size in samples (must be power of two)
    public const int MaxFrameSize = SpectralSubtractorConstants.MaxFrameSize;       // Maximum frame size in samples (must be power of two)
    public const int MinFramesForNoiseEstimation = 1;                               // Minimum number of frames that must fit in the noise estimation window
    public const double MinNoiseDurationSeconds = 0.001;                            // Shortest usable noise estimation window (~1 ms)
    public const double MaxNoiseDurationSeconds = 10.0;                             // Longest usable noise estimation window
}
