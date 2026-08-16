using System;

namespace SpectralDenoise;

/// <summary>
/// Centralised constants used by <see cref="SpectralSubtractorExtensions"/>.
/// </summary>
internal static class SpectralSubtractorExtensionsConstants
{
    // Argument validation messages
    public const string SignalEmptyMessage = "Signal cannot be empty.";
    public const string OutputEmptyMessage = "Output buffer cannot be empty.";
    public const string OutputTooSmallMessage = "Output buffer must be at least as long as input signal.";
    public const string NoiseProfileBinCountMismatchMessage = "Noise profile bin count does not match frame size.";
    public const string SilenceThresholdRangeMessage = "Silence threshold must be between 0.0 and 1.0";

    // Normalisation / RMS constants
    public const double TargetRms = 0.1;
    public const double MinRmsThreshold = 1e-10;

    // Silence‑detection defaults
    public const float DefaultSilenceThreshold = 0.01f;
    public const float NormalizationThreshold = 1e-6f;
}
