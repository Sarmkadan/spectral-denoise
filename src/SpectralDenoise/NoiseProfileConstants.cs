using System;

namespace SpectralDenoise;

/// <summary>
/// Centralised constants for <see cref="NoiseProfile"/> error messages and format strings.
/// </summary>
internal static class NoiseProfileConstants
{
    // Argument validation messages
    public const string ErrorMagnitudesNull = "Magnitudes array cannot be null.";
    public const string ErrorMagnitudesEmpty = "Magnitudes array cannot be empty.";
    public const string ErrorSampleRatePositive = "Sample rate must be positive.";
    public const string ErrorFrameSizePositive = "Frame size must be positive.";
    public const string ErrorHopPositive = "Hop must be positive.";

    // Validation format strings
    public const string ErrorMagnitudesLength = "Magnitudes array length must be {0} for frame size {1} (got {2}).";

    public const string ErrorSampleRateMismatch = "Sample rate mismatch: expected {0}Hz, got {1}Hz";
    public const string ErrorFrameSizeMismatch = "Frame size mismatch: expected {0}, got {1}";
    public const string ErrorHopMismatch = "Hop size mismatch: expected {0}, got {1}";
    public const string ErrorMagnitudeArrayLengthMismatch = "Magnitude array length mismatch: expected {0}, got {1}";
}
