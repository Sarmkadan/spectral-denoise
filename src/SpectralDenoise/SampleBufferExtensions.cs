using System;
using System.Linq;

namespace SpectralDenoise;

public static class SampleBufferExtensions
{
    /// <summary>
    /// Normalizes the samples so their peak amplitude matches the specified target.
    /// </summary>
    /// <param name="samples">The sample buffer to normalize.</param>
    /// <param name="targetPeak">The target peak amplitude.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="samples"/> is <see langword="null"/>.
    /// </exception>
    public static void NormalizePeak(this float[] samples, float targetPeak = 0.99f)
    {
        ArgumentNullException.ThrowIfNull(samples);
        if (samples.Length == 0) return;

        float max = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            float abs = Math.Abs(samples[i]);
            if (abs > max) max = abs;
        }

        if (max < 1e-9f) return;

        float factor = targetPeak / max;
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] *= factor;
        }
    }

    /// <summary>
    /// Calculates the root mean square level of the samples in decibels.
    /// </summary>
    /// <param name="samples">The sample buffer to measure.</param>
    /// <returns>The root mean square level in decibels.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="samples"/> is <see langword="null"/>.
    /// </exception>
    public static float RmsDb(this float[] samples)
    {
        ArgumentNullException.ThrowIfNull(samples);
        if (samples.Length == 0) return -120f;

        double sumSquares = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            sumSquares += (double)samples[i] * samples[i];
        }

        double rms = Math.Sqrt(sumSquares / samples.Length);

        if (rms < 1e-6f) return -120f;

        return (float)(20 * Math.Log10(rms));
    }

    /// <summary>
    /// Applies the specified gain in decibels to every sample in the buffer.
    /// </summary>
    /// <param name="samples">The sample buffer to modify.</param>
    /// <param name="gainDb">The gain to apply in decibels.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="samples"/> is <see langword="null"/>.
    /// </exception>
    public static void ApplyGainDb(this float[] samples, float gainDb)
    {
        ArgumentNullException.ThrowIfNull(samples);
        if (samples.Length == 0) return;

        float factor = (float)Math.Pow(10, gainDb / 20.0);
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] *= factor;
        }
    }

    /// <summary>
    /// Removes leading and trailing samples at or below the specified silence threshold.
    /// </summary>
    /// <param name="samples">The sample buffer to trim.</param>
    /// <param name="thresholdDb">The silence threshold in decibels.</param>
    /// <returns>A new buffer containing the samples above the silence threshold and the samples between them.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="samples"/> is <see langword="null"/>.
    /// </exception>
    public static float[] TrimSilence(this float[] samples, float thresholdDb = -50f)
    {
        ArgumentNullException.ThrowIfNull(samples);
        if (samples.Length == 0) return new float[0];

        float threshold = (float)Math.Pow(10, thresholdDb / 20.0);

        int start = 0;
        while (start < samples.Length && Math.Abs(samples[start]) <= threshold)
        {
            start++;
        }

        if (start == samples.Length) return new float[0];

        int end = samples.Length - 1;
        while (end > start && Math.Abs(samples[end]) <= threshold)
        {
            end--;
        }

        int length = end - start + 1;
        float[] trimmed = new float[length];
        Array.Copy(samples, start, trimmed, 0, length);
        return trimmed;
    }
}
