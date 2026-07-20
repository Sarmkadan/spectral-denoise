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

        // Validate Alpha (over-subtraction factor)
        // Should be >= 1.0 (1.0 = plain Boll, higher = more aggressive)
        if (value.Alpha < 1.0)
        {
            problems.Add(
                ValidationMessages.FormatParameterError(
                    nameof(value.Alpha),
                    $"must be ≥ 1.0 (over-subtraction factor, got " + value.Alpha.ToString(CultureInfo.InvariantCulture) + ")"));
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
}