using System;
using System.Linq;
using System.Numerics;

namespace SpectralDenoise;

/// <summary>
/// Validation messages for <see cref="SpectralSubtractor"/>.
/// </summary>
public static class SpectralSubtractorValidation
{
    /// <summary>
    /// Validates the frame size and hop size combination.
    /// </summary>
    /// <param name="subtractor">The spectral subtractor instance.</param>
    /// <returns>A list of validation messages.</returns>
    public static IReadOnlyList<string> ValidateFrameSizeAndHop(this SpectralSubtractor subtractor)
    {
        ArgumentNullException.ThrowIfNull(subtractor);

        var frameSize = subtractor.FrameSize;
        var hop = subtractor.Hop;

        if (frameSize <= 0)
            return new[] { "Frame size must be a positive integer." };

        if (hop <= 0)
            return new[] { "Hop size must be a positive integer." };

        if (frameSize % hop != 0)
            return new[] { "Frame size must be a multiple of hop size." };

        return Array.Empty<string>();
    }

    /// <summary>
    /// Validates the window and hop size combination.
    /// </summary>
    /// <param name="subtractor">The spectral subtractor instance.</param>
    /// <returns>A list of validation messages.</returns>
    public static IReadOnlyList<string> ValidateWindowAndHop(this SpectralSubtractor subtractor)
    {
        ArgumentNullException.ThrowIfNull(subtractor);

        var window = subtractor.Window;
        var hop = subtractor.Hop;

        if (window.Length != subtractor.FrameSize)
            return new[] { "Window length must match frame size." };

        if (!WindowFunctions.SatisfiesCola(window, hop))
            return new[] { "Window and hop size combination does not satisfy COLA." };

        return Array.Empty<string>();
    }
}
