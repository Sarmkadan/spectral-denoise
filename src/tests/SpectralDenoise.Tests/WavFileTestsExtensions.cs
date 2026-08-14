using System;
using System.Globalization;
using System.IO;

namespace SpectralDenoise.Tests;

/// <summary>
/// Extension helpers for <see cref="WavFileTests"/> that simplify creating temporary WAV files
/// and generating test audio data.
/// </summary>
public static class WavFileTestsExtensions
{
    /// <summary>
    /// Generates a sine‑wave sample array.
    /// </summary>
    /// <param name="tests">The test instance (unused, only to enable extension syntax).</param>
    /// <param name="frequency">Frequency of the sine wave in hertz. Must be positive.</param>
    /// <param name="durationSeconds">Length of the signal in seconds. Must be positive.</param>
    /// <param name="sampleRate">Sample rate in hertz. Must be positive.</param>
    /// <param name="amplitude">Peak amplitude, clamped to the range [-1, 1]. Default is 0.5.</param>
    /// <returns>A <see cref="float"/> array containing the generated samples.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="frequency"/>, <paramref name="durationSeconds"/> or <paramref name="sampleRate"/> is not positive.</exception>
    public static float[] GenerateSineWave(this WavFileTests tests, double frequency, double durationSeconds, int sampleRate, float amplitude = 0.5f)
    {
        if (frequency <= 0)
            throw new ArgumentException("Frequency must be positive.", nameof(frequency));
        if (durationSeconds <= 0)
            throw new ArgumentException("Duration must be positive.", nameof(durationSeconds));
        if (sampleRate <= 0)
            throw new ArgumentException("Sample rate must be positive.", nameof(sampleRate));

        // Clamp amplitude to the valid range for WAV files.
        amplitude = MathF.Max(-1.0f, MathF.Min(1.0f, amplitude));

        int sampleCount = (int)MathF.Round((float)(durationSeconds * sampleRate));
        var samples = new float[sampleCount];
        double increment = 2.0 * Math.PI * frequency / sampleRate;

        for (int i = 0; i < sampleCount; i++)
        {
            samples[i] = amplitude * MathF.Sin((float)(i * increment));
        }

        return samples;
    }

    /// <summary>
    /// Writes a mono WAV file to a temporary location and returns the file path.
    /// The caller is responsible for deleting the file when finished.
    /// </summary>
    /// <param name="tests">The test instance (unused, only to enable extension syntax).</param>
    /// <param name="samples">Mono audio samples to write. Must not be null.</param>
    /// <param name="sampleRate">Sample rate in hertz. Must be positive.</param>
    /// <returns>The full path of the created temporary WAV file.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="samples"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="sampleRate"/> is not positive.</exception>
    public static string CreateTempMonoWavFile(this WavFileTests tests, float[] samples, int sampleRate)
    {
        ArgumentNullException.ThrowIfNull(samples);
        if (sampleRate <= 0)
            throw new ArgumentException("Sample rate must be positive.", nameof(sampleRate));

        string tempPath = Path.ChangeExtension(Path.GetTempFileName(), ".wav");
        WavFile.WriteMono(tempPath, samples, sampleRate);
        return tempPath;
    }

    /// <summary>
    /// Writes a stereo WAV file to a temporary location and returns the file path.
    /// The caller is responsible for deleting the file when finished.
    /// </summary>
    /// <param name="tests">The test instance (unused, only to enable extension syntax).</param>
    /// <param name="left">Left‑channel samples. Must not be null.</param>
    /// <param name="right">Right‑channel samples. Must not be null.</param>
    /// <param name="sampleRate">Sample rate in hertz. Must be positive.</param>
    /// <returns>The full path of the created temporary WAV file.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="left"/> or <paramref name="right"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the channel arrays differ in length or when <paramref name="sampleRate"/> is not positive.</exception>
    public static string CreateTempStereoWavFile(this WavFileTests tests, float[] left, float[] right, int sampleRate)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        if (left.Length != right.Length)
            throw new ArgumentException("Left and right channel arrays must have the same length.", nameof(left));
        if (sampleRate <= 0)
            throw new ArgumentException("Sample rate must be positive.", nameof(sampleRate));

        string tempPath = Path.ChangeExtension(Path.GetTempFileName(), ".wav");
        WavFile.WriteStereo(tempPath, left, right, sampleRate);
        return tempPath;
    }
}
