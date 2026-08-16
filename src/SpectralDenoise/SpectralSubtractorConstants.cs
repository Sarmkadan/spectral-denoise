using System;

namespace SpectralDenoise;

/// <summary>
/// Centralised constants for <see cref="SpectralSubtractor"/> to avoid magic values scattered throughout the code.
/// </summary>
internal static class SpectralSubtractorConstants
{
    // ------------------------------------------------------------------------
    // Error / exception messages
    // ------------------------------------------------------------------------
    public const string HopColaErrorMessage =
        "Hop size {0} does not satisfy the Constant Overlap-Add (COLA) condition with the current window. " +
        "The sum of squared window values should be approximately equal to the hop size for perfect reconstruction. " +
        "Use a periodic window (e.g., WindowFunctions.HannPeriodic(frameSize)) and ensure hop size is compatible. " +
        "Common COLA‑compatible combinations: hop = frameSize/4 with periodic Hann, hop = frameSize/2 with periodic Hann.";

    public const string ValidateColaErrorMessage =
        "Window/overlap combination does not satisfy the Constant Overlap-Add (COLA) condition. " +
        "The sum of squared window values is {0:F6}, but should be approximately {1} for perfect reconstruction. " +
        "This causes amplitude modulation artifacts in the output. " +
        "Use a periodic window (e.g., WindowFunctions.HannPeriodic(frameSize)) and ensure hop size is compatible. " +
        "Common COLA‑compatible combinations: hop = frameSize/4 with periodic Hann, hop = frameSize/2 with periodic Hann.";

    public const string FrameSizePowerOfTwoMessage = "frameSize must be a power of two.";
    public const string HopPositiveMessage = "Hop must be positive.";
    public const string NoiseRegionTooShortMessage = "Noise region shorter than one frame - give me more leading silence.";

    // ------------------------------------------------------------------------
    // Numeric constants
    // ------------------------------------------------------------------------
    public const int DefaultSampleRate = 44_100;          // Standard CD‑quality sample rate used for time‑constant calculations
    public const double NormalizationEpsilon = 1e-6;      // Small value to avoid division by zero when normalising output
    public const double PowerEpsilon = 1e-20;             // Threshold to avoid divide‑by‑zero in SNR calculations
    public const int MinFrameSize = 128;                  // Minimum allowed frame size (must be power of two)
    public const int MaxFrameSize = 8_192;                // Maximum allowed frame size (must be power of two)
}
