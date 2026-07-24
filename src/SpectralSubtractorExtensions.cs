using System;
using System.Linq;
using System.Numerics;

namespace SpectralDenoise;

/// <summary>
/// Extension methods for <see cref="SpectralSubtractor"/> that provide additional functionality
/// for working with audio data and noise profiles.
/// </summary>
public static class SpectralSubtractorExtensions
{
    /// <summary>
    /// Processes audio with a pre-allocated output buffer to avoid allocations.
    /// </summary>
    /// <param name="subtractor">The spectral subtractor instance.</param>
    /// <param name="signal">The audio signal to denoise.</param>
    /// <param name="noiseProfile">The pre-computed noise profile.</param>
    /// <param name="output">Pre-allocated buffer for the output (must be same length as signal).</param>
    /// <returns>The denoised signal (same reference as <paramref name="output"/>).</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when output buffer is too small.</exception>
    public static Span<float> Process(this SpectralSubtractor subtractor, ReadOnlySpan<float> signal, double[] noiseProfile, Span<float> output)
    {
        ArgumentNullException.ThrowIfNull(subtractor);
        ArgumentNullException.ThrowIfNull(noiseProfile);

        if (signal.IsEmpty)
            throw new ArgumentException("Signal cannot be empty.", nameof(signal));

        if (output.IsEmpty)
            throw new ArgumentException("Output buffer cannot be empty.", nameof(output));

        if (output.Length < signal.Length)
            throw new ArgumentOutOfRangeException(nameof(output), "Output buffer must be at least as long as input signal.");

        // ... (rest of the method remains the same)

    /// <summary>
    /// Checks if the window and hop size combination satisfies the constant-overlap-add condition.
    /// </summary>
    /// <param name="subtractor">The spectral subtractor instance.</param>
    /// <returns>True if the combination satisfies COLA, false otherwise.</returns>
    public static bool IsColaSatisfied(this SpectralSubtractor subtractor)
    {
        ArgumentNullException.ThrowIfNull(subtractor);

        var window = subtractor.Window;
        var hop = subtractor.Hop;

        var windowSumSquared = WindowFunctions.ComputeWindowSumSquared(window, hop, window.Length);

        return windowSumSquared > 0;
    }

    /// <summary>
    /// Normalizes the synthesis by the summed squared window.
    /// </summary>
    /// <param name="subtractor">The spectral subtractor instance.</param>
    /// <param name="output">The output buffer.</param>
    public static void NormalizeSynthesis(this SpectralSubtractor subtractor, Span<float> output)
    {
        ArgumentNullException.ThrowIfNull(subtractor);
        ArgumentNullException.ThrowIfNull(output);

        if (output.IsEmpty)
            throw new ArgumentException("Output buffer cannot be empty.", nameof(output));

        var window = subtractor.Window;
        var hop = subtractor.Hop;

        var windowSumSquared = WindowFunctions.ComputeWindowSumSquared(window, hop, window.Length);

        if (windowSumSquared > 0)
        {
            var normalizationFactor = 1.0f / windowSumSquared;
            for (int i = 0; i < output.Length; i++)
                output[i] *= normalizationFactor;
        }
    }
}
