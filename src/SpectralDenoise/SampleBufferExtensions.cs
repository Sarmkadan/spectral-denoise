using System;
using System.Linq;

namespace SpectralDenoise;

public static class SampleBufferExtensions
{
    public static void NormalizePeak(this float[] samples, float targetPeak = 0.99f)
    {
        if (samples == null || samples.Length == 0) return;

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

    public static float RmsDb(this float[] samples)
    {
        if (samples == null || samples.Length == 0) return -120f;

        double sumSquares = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            sumSquares += (double)samples[i] * samples[i];
        }

        double rms = Math.Sqrt(sumSquares / samples.Length);

        if (rms < 1e-6f) return -120f;

        return (float)(20 * Math.Log10(rms));
    }

    public static void ApplyGainDb(this float[] samples, float gainDb)
    {
        if (samples == null || samples.Length == 0) return;

        float factor = (float)Math.Pow(10, gainDb / 20.0);
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] *= factor;
        }
    }

    public static float[] TrimSilence(this float[] samples, float thresholdDb = -50f)
    {
        if (samples == null || samples.Length == 0) return new float[0];

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
