using System;
using System.Collections.Generic;

namespace SpectralDenoise;

/// <summary>
/// Compatibility wrapper for validation helpers. The core validation now lives in <see cref="SpectralSubtractor"/>.
/// </summary>
public static class SpectralSubtractorValidation
{
    /// <summary>
    /// Validates the frame size and hop size combination.
    /// </summary>
    /// <param name="subtractor">The spectral subtractor instance.</param>
    /// <returns>A list of validation messages.</returns>
    public static IReadOnlyList<string> ValidateFrameSizeAndHop(this SpectralSubtractor subtractor) =>
        subtractor.ValidateFrameSizeAndHop();

    /// <summary>
    /// Validates the window and hop size combination.
    /// </summary>
    /// <param name="subtractor">The spectral subtractor instance.</param>
    /// <returns>A list of validation messages.</returns>
    public static IReadOnlyList<string> ValidateWindowAndHop(this SpectralSubtractor subtractor) =>
        subtractor.ValidateWindowAndHop();
}
