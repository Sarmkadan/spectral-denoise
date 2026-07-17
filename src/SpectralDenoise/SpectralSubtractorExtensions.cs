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

        // Copy signal to output buffer
        signal.CopyTo(output);

        // Process in-place on the output buffer
        var result = subtractor.Process(output, noiseProfile);

        return output.Slice(0, signal.Length);
    }

    /// <summary>
    /// Creates a normalized noise profile from a noise sample, ensuring the profile
    /// has consistent magnitude across all frequency bins.
    /// </summary>
    /// <param name="subtractor">The spectral subtractor instance.</param>
    /// <param name="noiseOnly">Noise-only sample to estimate profile from.</param>
    /// <param name="normalize">Whether to normalize the profile to a target RMS level (default: true).</param>
    /// <returns>A normalized noise profile suitable for use with <see cref="SpectralSubtractor.Process"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="noiseOnly"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when noise region is too short.</exception>
    public static double[] EstimateNormalizedNoiseProfile(this SpectralSubtractor subtractor, ReadOnlySpan<float> noiseOnly, bool normalize = true)
    {
        ArgumentNullException.ThrowIfNull(subtractor);
        if (noiseOnly == null) throw new ArgumentNullException(nameof(noiseOnly));

        var profile = subtractor.EstimateNoiseProfile(noiseOnly);

        if (normalize)
        {
            // Normalize to target RMS of 0.1 (arbitrary but reasonable for audio)
            const double targetRms = 0.1;
            double rms = Math.Sqrt(profile.Sum(p => p * p) / profile.Length);

            if (rms > 1e-10)
            {
                double scale = targetRms / rms;
                for (int i = 0; i < profile.Length; i++)
                    profile[i] *= scale;
            }
        }

        return profile;
    }

    /// <summary>
    /// Processes audio with automatic silence detection - skips processing frames
    /// that fall below a specified energy threshold.
    /// </summary>
    /// <param name="subtractor">The spectral subtractor instance.</param>
    /// <param name="signal">The audio signal to denoise.</param>
    /// <param name="noiseProfile">The pre-computed noise profile.</param>
    /// <param name="silenceThreshold">Energy threshold below which frames are skipped (0.0 to 1.0).</param>
    /// <returns>The denoised signal.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="silenceThreshold"/> is outside valid range.</exception>
    public static float[] ProcessWithSilenceDetection(this SpectralSubtractor subtractor, ReadOnlySpan<float> signal, double[] noiseProfile, float silenceThreshold = 0.01f)
    {
        ArgumentNullException.ThrowIfNull(subtractor);
        ArgumentNullException.ThrowIfNull(subtractor);
        if (signal == null) throw new ArgumentNullException(nameof(signal));
        ArgumentNullException.ThrowIfNull(noiseProfile);

        if (silenceThreshold is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(silenceThreshold), "Silence threshold must be between 0.0 and 1.0");

        int bins = subtractor.GetFrameSize() / 2 + 1;
        if (noiseProfile.Length != bins)
            throw new ArgumentException("Noise profile bin count does not match frame size.");

        var output = new float[signal.Length];
        var normalization = new float[signal.Length];

        for (int start = 0; start + subtractor.GetFrameSize() <= signal.Length; start += subtractor.GetHopSize())
        {
            // Check if frame is silent
            bool isSilent = true;
            for (int i = 0; i < subtractor.GetFrameSize(); i++)
            {
                if (Math.Abs(signal[start + i]) > silenceThreshold)
                {
                    isSilent = false;
                    break;
                }
            }

            if (isSilent)
                continue; // Skip silent frames

            // Use the public Process method for actual denoising
            var frameOutput = subtractor.Process(signal.Slice(start, Math.Min(subtractor.GetFrameSize(), signal.Length - start)), noiseProfile);

            // Accumulate the result
            for (int i = 0; i < frameOutput.Length; i++)
            {
                output[start + i] += frameOutput[i];
                normalization[start + i] += 1.0f;
            }
        }

        // undo the analysis+synthesis window weighting
        const float normalizationThreshold = 1e-6f;
        for (int i = 0; i < output.Length; i++)
            if (normalization[i] > normalizationThreshold)
                output[i] /= normalization[i];

        return output;
    }

    /// <summary>
    /// Gets the frame size used by this subtractor instance.
    /// </summary>
    /// <param name="subtractor">The spectral subtractor instance.</param>
    /// <returns>The frame size in samples.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="subtractor"/> is null.</exception>
    public static int GetFrameSize(this SpectralSubtractor subtractor)
    {
        ArgumentNullException.ThrowIfNull(subtractor);
        return subtractor.GetFrameSize();
    }

    /// <summary>
    /// Gets the hop size (frame advance) used by this subtractor instance.
    /// </summary>
    /// <param name="subtractor">The spectral subtractor instance.</param>
    /// <returns>The hop size in samples.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="subtractor"/> is null.</exception>
    public static int GetHopSize(this SpectralSubtractor subtractor)
    {
        ArgumentNullException.ThrowIfNull(subtractor);
        return subtractor.GetHopSize();
    }

    /// <summary>
    /// Gets the analysis window used by this subtractor instance.
    /// </summary>
    /// <param name="subtractor">The spectral subtractor instance.</param>
    /// <returns>The window function as an array.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="subtractor"/> is null.</exception>
    public static double[] GetWindow(this SpectralSubtractor subtractor)
    {
        ArgumentNullException.ThrowIfNull(subtractor);
        return subtractor.GetWindow();
    }
}
