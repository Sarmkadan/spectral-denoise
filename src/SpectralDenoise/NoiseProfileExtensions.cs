using System;
using System.Collections.Generic;
using System.Globalization;

namespace SpectralDenoise;

/// <summary>
/// Provides extension methods for working with <see cref="NoiseProfile"/> instances.
/// </summary>
public static class NoiseProfileExtensions
{
    /// <summary>
    /// Gets the frequency resolution of this noise profile in Hz per bin.
    /// </summary>
    /// <param name="profile">The noise profile.</param>
    /// <returns>The frequency resolution in Hz per bin.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="profile"/> is null.</exception>
    public static double GetFrequencyResolution(this NoiseProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return (double)profile.SampleRate / profile.FrameSize;
    }

    /// <summary>
    /// Gets the highest frequency represented in this noise profile in Hz.
    /// </summary>
    /// <param name="profile">The noise profile.</param>
    /// <returns>The highest frequency in Hz.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="profile"/> is null.</exception>
    public static double GetHighestFrequency(this NoiseProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return profile.GetFrequencyResolution() * (profile.FrameSize / 2);
    }

    /// <summary>
    /// Gets the frequency for a specific frequency bin index.
    /// </summary>
    /// <param name="profile">The noise profile.</param>
    /// <param name="binIndex">The frequency bin index (0 to FrameSize/2).</param>
    /// <returns>The frequency in Hz for the specified bin.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="profile"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="binIndex"/> is out of range.</exception>
    public static double GetFrequencyForBin(this NoiseProfile profile, int binIndex)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (binIndex < 0 || binIndex >= profile.Magnitudes.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(binIndex),
                $"Bin index must be between 0 and {profile.Magnitudes.Length - 1}.");
        }

        return profile.GetFrequencyResolution() * binIndex;
    }

    /// <summary>
    /// Gets the magnitude at a specific frequency bin index.
    /// </summary>
    /// <param name="profile">The noise profile.</param>
    /// <param name="binIndex">The frequency bin index (0 to FrameSize/2).</param>
    /// <returns>The magnitude at the specified bin.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="profile"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="binIndex"/> is out of range.</exception>
    public static double GetMagnitudeAtBin(this NoiseProfile profile, int binIndex)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (binIndex < 0 || binIndex >= profile.Magnitudes.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(binIndex),
                $"Bin index must be between 0 and {profile.Magnitudes.Length - 1}.");
        }

        return profile.Magnitudes[binIndex];
    }

    /// <summary>
    /// Gets an enumerable of frequency-magnitude pairs for all bins in the noise profile.
    /// </summary>
    /// <param name="profile">The noise profile.</param>
    /// <returns>An enumerable of (frequency, magnitude) tuples.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="profile"/> is null.</exception>
    public static IEnumerable<(double Frequency, double Magnitude)> GetFrequencyMagnitudePairs(
        this NoiseProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        double frequencyResolution = profile.GetFrequencyResolution();

        for (int i = 0; i < profile.Magnitudes.Length; i++)
        {
            yield return (frequencyResolution * i, profile.Magnitudes[i]);
        }
    }

    /// <summary>
    /// Creates a new noise profile with magnitudes scaled by a constant factor.
    /// </summary>
    /// <param name="profile">The original noise profile.</param>
    /// <param name="scaleFactor">The factor to multiply all magnitudes by.</param>
    /// <returns>A new noise profile with scaled magnitudes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="profile"/> is null.</exception>
    public static NoiseProfile ScaleMagnitudes(this NoiseProfile profile, double scaleFactor)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (scaleFactor < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(scaleFactor),
                "Scale factor must be non-negative.");
        }

        var scaledMagnitudes = new double[profile.Magnitudes.Length];
        for (int i = 0; i < profile.Magnitudes.Length; i++)
        {
            scaledMagnitudes[i] = profile.Magnitudes[i] * scaleFactor;
        }

        return new NoiseProfile(
            scaledMagnitudes,
            profile.SampleRate,
            profile.FrameSize,
            profile.Hop);
    }

    /// <summary>
    /// Creates a new noise profile with magnitudes clamped to a minimum and maximum value.
    /// </summary>
    /// <param name="profile">The original noise profile.</param>
    /// <param name="minValue">The minimum allowed magnitude value (inclusive).</param>
    /// <param name="maxValue">The maximum allowed magnitude value (inclusive).</param>
    /// <returns>A new noise profile with clamped magnitudes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="profile"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when minValue is greater than maxValue.</exception>
    public static NoiseProfile ClampMagnitudes(
        this NoiseProfile profile,
        double minValue,
        double maxValue)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (minValue > maxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minValue),
                "Minimum value cannot be greater than maximum value.");
        }

        var clampedMagnitudes = new double[profile.Magnitudes.Length];
        for (int i = 0; i < profile.Magnitudes.Length; i++)
        {
            clampedMagnitudes[i] = Math.Clamp(profile.Magnitudes[i], minValue, maxValue);
        }

        return new NoiseProfile(
            clampedMagnitudes,
            profile.SampleRate,
            profile.FrameSize,
            profile.Hop);
    }

    /// <summary>
    /// Gets the total noise energy across all frequency bins.
    /// </summary>
    /// <param name="profile">The noise profile.</param>
    /// <returns>The sum of squared magnitudes (energy).</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="profile"/> is null.</exception>
    public static double GetTotalEnergy(this NoiseProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        double energy = 0.0;
        for (int i = 0; i < profile.Magnitudes.Length; i++)
        {
            energy += profile.Magnitudes[i] * profile.Magnitudes[i];
        }

        return energy;
    }

    /// <summary>
    /// Gets the average magnitude across all frequency bins.
    /// </summary>
    /// <param name="profile">The noise profile.</param>
    /// <returns>The average magnitude.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="profile"/> is null.</exception>
    public static double GetAverageMagnitude(this NoiseProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (profile.Magnitudes.Length == 0)
        {
            return 0.0;
        }

        return profile.GetTotalEnergy() / profile.Magnitudes.Length;
    }

    /// <summary>
    /// Gets the maximum magnitude across all frequency bins and its corresponding frequency.
    /// </summary>
    /// <param name="profile">The noise profile.</param>
    /// <returns>A tuple containing the maximum magnitude and its frequency in Hz.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="profile"/> is null.</exception>
    public static (double MaxMagnitude, double Frequency) GetPeakMagnitude(
        this NoiseProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (profile.Magnitudes.Length == 0)
        {
            return (0.0, 0.0);
        }

        double maxMagnitude = profile.Magnitudes[0];
        int maxIndex = 0;

        for (int i = 1; i < profile.Magnitudes.Length; i++)
        {
            if (profile.Magnitudes[i] > maxMagnitude)
            {
                maxMagnitude = profile.Magnitudes[i];
                maxIndex = i;
            }
        }

        double frequency = profile.GetFrequencyForBin(maxIndex);
        return (maxMagnitude, frequency);
    }

    /// <summary>
    /// Creates a new noise profile with magnitudes normalized to a maximum value.
    /// </summary>
    /// <param name="profile">The original noise profile.</param>
    /// <param name="maxNormalizedValue">The maximum value after normalization.</param>
    /// <returns>A new noise profile with normalized magnitudes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="profile"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxNormalizedValue"/> is not positive.</exception>
    public static NoiseProfile NormalizeMagnitudes(
        this NoiseProfile profile,
        double maxNormalizedValue = 1.0)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (maxNormalizedValue <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxNormalizedValue),
                "Maximum normalized value must be positive.");
        }

        double maxMagnitude = profile.GetPeakMagnitude().MaxMagnitude;

        if (maxMagnitude == 0)
        {
            // Already silent, return a copy with zero magnitudes
            return new NoiseProfile(
                new double[profile.Magnitudes.Length],
                profile.SampleRate,
                profile.FrameSize,
                profile.Hop);
        }

        var normalizedMagnitudes = new double[profile.Magnitudes.Length];
        double scaleFactor = maxNormalizedValue / maxMagnitude;

        for (int i = 0; i < profile.Magnitudes.Length; i++)
        {
            normalizedMagnitudes[i] = profile.Magnitudes[i] * scaleFactor;
        }

        return new NoiseProfile(
            normalizedMagnitudes,
            profile.SampleRate,
            profile.FrameSize,
            profile.Hop);
    }
}