using System;
using System.Collections.Generic;
using System.Globalization;

namespace SpectralDenoise;

/// <summary>
/// Provides validation methods for <see cref="SpectralSubtractor"/> instances and noise profiles.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
internal static class ValidationMessages
{
    /// <summary>
    /// Formats a validation error message for a parameter.
    /// </summary>
    /// <param name="paramName">The parameter name.</param>
    /// <param name="message">The specific validation message.</param>
    /// <returns>A formatted error message.</returns>
    internal static string FormatParameterError(string paramName, string message)
        => $"Parameter '{paramName}' {message}.";

    /// <summary>
    /// Formats a collection validation error message.
    /// </summary>
    /// <param name="collectionName">The collection name.</param>
    /// <param name="index">The collection index.</param>
    /// <param name="message">The specific validation message.</param>
    /// <returns>A formatted error message.</returns>
    internal static string FormatCollectionError(string collectionName, int? index, string message)
        => index.HasValue
            ? $"Parameter '{collectionName}[{index.Value}]' {message}."
            : $"Parameter '{collectionName}' {message}.";
}

/// <summary>
/// Provides validation methods for <see cref="SpectralSubtractor"/> instances.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class SpectralSubtractorValidation
{
    /// <summary>
    /// Validates a <see cref="SpectralSubtractor"/> instance for common problems.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <returns>A list of human-readable problem descriptions; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this SpectralSubtractor? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate OverSubtractionFactor
        // Should be >= 1.0 (1.0 = plain Boll, higher = more aggressive)
        if (value.OverSubtractionFactor < 1.0)
        {
            problems.Add(
                ValidationMessages.FormatParameterError(
                    "Alpha",
                    $"must be ≥ 1.0 (over-subtraction factor, got " + value.OverSubtractionFactor.ToString(CultureInfo.InvariantCulture) + ")"));
        }

        // Validate SpectralFloor (spectral floor)
        // Should be in range [0, 1] (fraction of original magnitude to mask musical noise)
        if (value.SpectralFloor is < 0.0 or > 1.0)
        {
            problems.Add(
                ValidationMessages.FormatParameterError(
                    nameof(value.SpectralFloor),
                    $"must be in range [0, 1] (spectral floor, got " + value.SpectralFloor.ToString(CultureInfo.InvariantCulture) + "]"));
        }

        return problems;
    }

    /// <summary>
    /// Checks whether a <see cref="SpectralSubtractor"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to check.</param>
    /// <returns>True if valid; otherwise false.</returns>
    public static bool IsValid(this SpectralSubtractor? value)
        => value?.Validate().Count == 0;

    /// <summary>
    /// Ensures that a <see cref="SpectralSubtractor"/> instance is valid, throwing an <see cref="ArgumentException"/>
    /// with a detailed message listing all problems if it is not.
    /// </summary>
    /// <param name="value">The instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the instance is invalid, containing a list of problems.</exception>
    public static void EnsureValid(this SpectralSubtractor? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"SpectralSubtractor is invalid:{Environment.NewLine} - {string.Join($"{Environment.NewLine} - ", problems)}");
    }

    /// <summary>
    /// Validates a noise profile array for common problems.
    /// </summary>
    /// <param name="noiseProfile">The noise profile to validate.</param>
    /// <param name="paramName">The name of the parameter for error messages.</param>
    /// <returns>A list of human-readable problem descriptions; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="noiseProfile"/> is null.</exception>
    public static IReadOnlyList<string> ValidateNoiseProfile(this double[]? noiseProfile, string paramName = "noiseProfile")
    {
        ArgumentNullException.ThrowIfNull(noiseProfile);

        var problems = new List<string>();

        // Check for empty array
        if (noiseProfile.Length == 0)
        {
            problems.Add(ValidationMessages.FormatCollectionError(paramName, null, "must not be empty"));
        }

        // Check for NaN or infinity values
        for (int i = 0; i < noiseProfile.Length; i++)
        {
            if (double.IsNaN(noiseProfile[i]))
            {
                problems.Add(ValidationMessages.FormatCollectionError(paramName, i, "must not be NaN"));
            }
            else if (double.IsInfinity(noiseProfile[i]))
            {
                problems.Add(ValidationMessages.FormatCollectionError(paramName, i, "must not be infinite"));
            }
            else if (noiseProfile[i] < 0.0)
            {
                problems.Add(
                    ValidationMessages.FormatCollectionError(
                        paramName,
                        i,
                        $"must not be negative (got " + noiseProfile[i].ToString(CultureInfo.InvariantCulture) + ")"));
            }
        }

        return problems;
    }

    /// <summary>
    /// Checks whether a noise profile array is valid.
    /// </summary>
    /// <param name="noiseProfile">The noise profile to check.</param>
    /// <returns>True if valid; otherwise false.</returns>
    public static bool IsValidNoiseProfile(this double[]? noiseProfile)
        => noiseProfile?.ValidateNoiseProfile().Count == 0;

    /// <summary>
    /// Ensures that a noise profile array is valid, throwing an <see cref="ArgumentException"/>
    /// with a detailed message listing all problems if it is not.
    /// </summary>
    /// <param name="noiseProfile">The noise profile to validate.</param>
    /// <param name="paramName">The name of the parameter for error messages.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="noiseProfile"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the noise profile is invalid, containing a list of problems.</exception>
    public static void EnsureValidNoiseProfile(this double[]? noiseProfile, string paramName = "noiseProfile")
    {
        ArgumentNullException.ThrowIfNull(noiseProfile);

        var problems = noiseProfile.ValidateNoiseProfile(paramName);
        if (problems.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"Noise profile is invalid:{Environment.NewLine} - {string.Join($"{Environment.NewLine} - ", problems)}");
    }

    /// <summary>
    /// Validates that a sample rate is supported by the pipeline.
    /// </summary>
    /// <param name="sampleRate">The sample rate in Hz.</param>
    /// <returns>A list of human-readable problem descriptions; empty if valid.</returns>
    public static IReadOnlyList<string> ValidateSampleRate(int sampleRate)
    {
        var problems = new List<string>();

        const int MinSampleRate = 8000;
        const int MaxSampleRate = 192000;

        if (sampleRate < MinSampleRate || sampleRate > MaxSampleRate)
        {
            problems.Add(ValidationMessages.FormatParameterError(
                nameof(sampleRate),
                $"must be between {MinSampleRate} Hz and {MaxSampleRate} Hz (got {sampleRate} Hz)."));
        }

        return problems;
    }

    /// <summary>
    /// Checks whether a sample rate is supported by the pipeline.
    /// </summary>
    /// <param name="sampleRate">The sample rate in Hz.</param>
    /// <returns>True if valid; otherwise false.</returns>
    public static bool IsValidSampleRate(int sampleRate) => ValidateSampleRate(sampleRate).Count == 0;

    /// <summary>
    /// Ensures that a sample rate is supported, throwing an <see cref="ArgumentException"/>
    /// with a detailed message if it is not.
    /// </summary>
    /// <param name="sampleRate">The sample rate in Hz.</param>
    /// <exception cref="ArgumentException">Thrown when the sample rate is invalid.</exception>
    public static void EnsureValidSampleRate(int sampleRate)
    {
        var problems = ValidateSampleRate(sampleRate);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"Sample rate is invalid:{Environment.NewLine} - {string.Join($"{Environment.NewLine} - ", problems)}");
        }
    }

    /// <summary>
    /// Validates that a signal length is sufficient for processing.
    /// </summary>
    /// <param name="signalLength">The length of the signal in samples.</param>
    /// <param name="frameSize">The frame size used for processing.</param>
    /// <returns>A list of human-readable problem descriptions; empty if valid.</returns>
    public static IReadOnlyList<string> ValidateSignalLength(int signalLength, int frameSize)
    {
        var problems = new List<string>();

        if (signalLength <= 0)
        {
            problems.Add(ValidationMessages.FormatParameterError(
                nameof(signalLength),
                $"must be positive (got {signalLength})."));
        }
        else if (signalLength < frameSize)
        {
            problems.Add(ValidationMessages.FormatParameterError(
                nameof(signalLength),
                $"must be at least {frameSize} samples to process one frame (got {signalLength} samples)."));
        }

        return problems;
    }

    /// <summary>
    /// Checks whether a signal length is sufficient for processing.
    /// </summary>
    /// <param name="signalLength">The length of the signal in samples.</param>
    /// <param name="frameSize">The frame size used for processing.</param>
    /// <returns>True if valid; otherwise false.</returns>
    public static bool IsValidSignalLength(int signalLength, int frameSize) => ValidateSignalLength(signalLength, frameSize).Count == 0;

    /// <summary>
    /// Ensures that a signal length is sufficient for processing, throwing an <see cref="ArgumentException"/>
    /// with a detailed message if it is not.
    /// </summary>
    /// <param name="signalLength">The length of the signal in samples.</param>
    /// <param name="frameSize">The frame size used for processing.</param>
    /// <exception cref="ArgumentException">Thrown when the signal length is invalid.</exception>
    public static void EnsureValidSignalLength(int signalLength, int frameSize)
    {
        var problems = ValidateSignalLength(signalLength, frameSize);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"Signal length is invalid:{Environment.NewLine} - {string.Join($"{Environment.NewLine} - ", problems)}");
        }
    }

    /// <summary>
    /// Validates that a frame size is a power of two.
    /// </summary>
    /// <param name="frameSize">The frame size in samples.</param>
    /// <returns>A list of human-readable problem descriptions; empty if valid.</returns>
    public static IReadOnlyList<string> ValidateFrameSize(int frameSize)
    {
        var problems = new List<string>();

        if (frameSize <= 0)
        {
            problems.Add(ValidationMessages.FormatParameterError(
                nameof(frameSize),
                $"must be positive (got {frameSize})."));
        }
        else if (!IsPowerOfTwo(frameSize))
        {
            problems.Add(ValidationMessages.FormatParameterError(
                nameof(frameSize),
                $"must be a power of two (got {frameSize})."));
        }
        else if (frameSize < 128 || frameSize > 8192)
        {
            problems.Add(ValidationMessages.FormatParameterError(
                nameof(frameSize),
                $"should be between 128 and 8192 samples (got {frameSize})."));
        }

        return problems;
    }

    /// <summary>
    /// Checks whether a frame size is valid.
    /// </summary>
    /// <param name="frameSize">The frame size in samples.</param>
    /// <returns>True if valid; otherwise false.</returns>
    public static bool IsValidFrameSize(int frameSize) => ValidateFrameSize(frameSize).Count == 0;

    /// <summary>
    /// Ensures that a frame size is valid, throwing an <see cref="ArgumentException"/>
    /// with a detailed message if it is not.
    /// </summary>
    /// <param name="frameSize">The frame size in samples.</param>
    /// <exception cref="ArgumentException">Thrown when the frame size is invalid.</exception>
    public static void EnsureValidFrameSize(int frameSize)
    {
        var problems = ValidateFrameSize(frameSize);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"Frame size is invalid:{Environment.NewLine} - {string.Join($"{Environment.NewLine} - ", problems)}");
        }
    }

    /// <summary>
    /// Validates that a hop size is compatible with a window function.
    /// </summary>
    /// <param name="hop">The hop size in samples.</param>
    /// <param name="window">The window function to check against.</param>
    /// <returns>A list of human-readable problem descriptions; empty if valid.</returns>
    public static IReadOnlyList<string> ValidateHopSize(int hop, ReadOnlySpan<double> window)
    {
        var problems = new List<string>();

        if (hop <= 0)
        {
            problems.Add(ValidationMessages.FormatParameterError(
                nameof(hop),
                $"must be positive (got {hop})."));
        }
        else if (!WindowFunctions.SatisfiesCola(window, hop))
        {
            double sum = 0.0;
            for (int i = 0; i < window.Length; i++)
            {
                sum += window[i] * window[i];
            }

            problems.Add(ValidationMessages.FormatParameterError(
                nameof(hop),
                $"must satisfy the Constant Overlap-Add (COLA) condition with the window function. " +
                $"The sum of squared window values is {sum:F6}, but should be approximately {hop} for perfect reconstruction. " +
                $"Use a periodic window (e.g., WindowFunctions.HannPeriodic(frameSize)) and ensure hop size is compatible. " +
                $"Common COLA-compatible combinations: hop = frameSize/4, hop = frameSize/2."));
        }

        return problems;
    }

    /// <summary>
    /// Checks whether a hop size is valid for a given window.
    /// </summary>
    /// <param name="hop">The hop size in samples.</param>
    /// <param name="window">The window function to check against.</param>
    /// <returns>True if valid; otherwise false.</returns>
    public static bool IsValidHopSize(int hop, ReadOnlySpan<double> window) => ValidateHopSize(hop, window).Count == 0;

    /// <summary>
    /// Ensures that a hop size is valid for a given window, throwing an <see cref="ArgumentException"/>
    /// with a detailed message if it is not.
    /// </summary>
    /// <param name="hop">The hop size in samples.</param>
    /// <param name="window">The window function to check against.</param>
    /// <exception cref="ArgumentException">Thrown when the hop size is invalid.</exception>
    public static void EnsureValidHopSize(int hop, ReadOnlySpan<double> window)
    {
        var problems = ValidateHopSize(hop, window);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"Hop size is invalid:{Environment.NewLine} - {string.Join($"{Environment.NewLine} - ", problems)}");
        }
    }

    /// <summary>
    /// Validates that a noise estimation duration is reasonable.
    /// </summary>
    /// <param name="durationSeconds">The noise estimation duration in seconds.</param>
    /// <param name="sampleRate">The audio sample rate in Hz.</param>
    /// <returns>A list of human-readable problem descriptions; empty if valid.</returns>
    public static IReadOnlyList<string> ValidateNoiseEstimationDuration(double durationSeconds, int sampleRate)
    {
        var problems = new List<string>();

        if (durationSeconds <= 0)
        {
            problems.Add(ValidationMessages.FormatParameterError(
                nameof(durationSeconds),
                $"must be positive (got {durationSeconds:F3} s)."));
        }
        else if (durationSeconds < 0.001) // Less than 1 ms
        {
            problems.Add(ValidationMessages.FormatParameterError(
                nameof(durationSeconds),
                $"is too short (got {durationSeconds:F3} s, minimum ~1 ms)."));
        }
        else if (durationSeconds > 10.0) // More than 10 seconds
        {
            problems.Add(ValidationMessages.FormatParameterError(
                nameof(durationSeconds),
                $"is too long (got {durationSeconds:F3} s, maximum 10 s). " +
                $"Consider using a more sophisticated noise estimation algorithm for long durations."));
        }

        // Ensure at least one frame can be analyzed
        int minFrames = 1;
        int minSamples = minFrames * 128; // Minimum frame size
        int minDurationSamples = (int)(durationSeconds * sampleRate);
        if (minDurationSamples < minSamples)
        {
            problems.Add(ValidationMessages.FormatParameterError(
                nameof(durationSeconds),
                $"must provide at least {minFrames} frame(s) of audio for noise estimation " +
                $"(requires {minSamples} samples at {sampleRate} Hz, got {minDurationSamples} samples)."));
        }

        return problems;
    }

    /// <summary>
    /// Checks whether a noise estimation duration is valid.
    /// </summary>
    /// <param name="durationSeconds">The noise estimation duration in seconds.</param>
    /// <param name="sampleRate">The audio sample rate in Hz.</param>
    /// <returns>True if valid; otherwise false.</returns>
    public static bool IsValidNoiseEstimationDuration(double durationSeconds, int sampleRate)
        => ValidateNoiseEstimationDuration(durationSeconds, sampleRate).Count == 0;

    /// <summary>
    /// Ensures that a noise estimation duration is valid, throwing an <see cref="ArgumentException"/>
    /// with a detailed message if it is not.
    /// </summary>
    /// <param name="durationSeconds">The noise estimation duration in seconds.</param>
    /// <param name="sampleRate">The audio sample rate in Hz.</param>
    /// <exception cref="ArgumentException">Thrown when the duration is invalid.</exception>
    public static void EnsureValidNoiseEstimationDuration(double durationSeconds, int sampleRate)
    {
        var problems = ValidateNoiseEstimationDuration(durationSeconds, sampleRate);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"Noise estimation duration is invalid:{Environment.NewLine} - {string.Join($"{Environment.NewLine} - ", problems)}");
        }
    }

    /// <summary>
    /// Validates that the Alpha (over-subtraction factor) is within valid range.
    /// </summary>
    /// <param name="alpha">The over-subtraction factor.</param>
    /// <returns>A list of human-readable problem descriptions; empty if valid.</returns>
    public static IReadOnlyList<string> ValidateAlpha(double alpha)
    {
        var problems = new List<string>();

        if (alpha < 1.0)
        {
            problems.Add(ValidationMessages.FormatParameterError(
                nameof(alpha),
                $"must be ≥ 1.0 (over-subtraction factor, got {alpha:F4})."));
        }
        else if (double.IsNaN(alpha))
        {
            problems.Add(ValidationMessages.FormatParameterError(nameof(alpha), "must not be NaN."));
        }
        else if (double.IsInfinity(alpha))
        {
            problems.Add(ValidationMessages.FormatParameterError(nameof(alpha), "must not be infinite."));
        }

        return problems;
    }

    /// <summary>
    /// Checks whether the Alpha (over-subtraction factor) is valid.
    /// </summary>
    /// <param name="alpha">The over-subtraction factor.</param>
    /// <returns>True if valid; otherwise false.</returns>
    public static bool IsValidAlpha(double alpha) => ValidateAlpha(alpha).Count == 0;

    /// <summary>
    /// Ensures that the Alpha (over-subtraction factor) is valid, throwing an <see cref="ArgumentException"/>
    /// with a detailed message if it is not.
    /// </summary>
    /// <param name="alpha">The over-subtraction factor.</param>
    /// <exception cref="ArgumentException">Thrown when Alpha is invalid.</exception>
    public static void EnsureValidAlpha(double alpha)
    {
        var problems = ValidateAlpha(alpha);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"Alpha is invalid:{Environment.NewLine} - {string.Join($"{Environment.NewLine} - ", problems)}");
        }
    }

    /// <summary>
    /// Validates that the Beta (spectral floor) is within valid range [0, 1].
    /// </summary>
    /// <param name="beta">The spectral floor fraction.</param>
    /// <returns>A list of human-readable problem descriptions; empty if valid.</returns>
    public static IReadOnlyList<string> ValidateBeta(double beta)
    {
        var problems = new List<string>();

        if (beta < 0.0 || beta > 1.0)
        {
            problems.Add(ValidationMessages.FormatParameterError(
                nameof(beta),
                $"must be in range [0, 1] (spectral floor, got {beta:F4})."));
        }
        else if (double.IsNaN(beta))
        {
            problems.Add(ValidationMessages.FormatParameterError(nameof(beta), "must not be NaN."));
        }
        else if (double.IsInfinity(beta))
        {
            problems.Add(ValidationMessages.FormatParameterError(nameof(beta), "must not be infinite."));
        }

        return problems;
    }

    /// <summary>
    /// Checks whether the Beta (spectral floor) is valid.
    /// </summary>
    /// <param name="beta">The spectral floor fraction.</param>
    /// <returns>True if valid; otherwise false.</returns>
    public static bool IsValidBeta(double beta) => ValidateBeta(beta).Count == 0;

    /// <summary>
    /// Ensures that the Beta (spectral floor) is valid, throwing an <see cref="ArgumentException"/>
    /// with a detailed message if it is not.
    /// </summary>
    /// <param name="beta">The spectral floor fraction.</param>
    /// <exception cref="ArgumentException">Thrown when Beta is invalid.</exception>
    public static void EnsureValidBeta(double beta)
    {
        var problems = ValidateBeta(beta);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"Beta is invalid:{Environment.NewLine} - {string.Join($"{Environment.NewLine} - ", problems)}");
        }
    }

    /// <summary>
    /// Checks if a number is a power of two.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>True if the value is a power of two; otherwise false.</returns>
    private static bool IsPowerOfTwo(int value)
    {
        if (value <= 0)
            return false;

        return (value & (value - 1)) == 0;
    }
}
