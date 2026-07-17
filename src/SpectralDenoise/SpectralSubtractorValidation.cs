using System;
using System.Collections.Generic;
using System.Globalization;

namespace SpectralDenoise;

/// <summary>
/// Validation helpers for <see cref="SpectralSubtractor"/> instances and their parameters.
/// </summary>
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
                $"Alpha ({value.Alpha.ToString(CultureInfo.InvariantCulture)}) must be ≥ 1.0 (over-subtraction factor).");
        }

        // Validate Beta (spectral floor)
        // Should be in range [0, 1] (fraction of original magnitude to mask musical noise)
        if (value.Beta < 0.0 || value.Beta > 1.0)
        {
            problems.Add(
                $"Beta ({value.Beta.ToString(CultureInfo.InvariantCulture)}) must be in range [0, 1] (spectral floor).");
        }

        return problems;
    }

    /// <summary>
    /// Checks whether a <see cref="SpectralSubtractor"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to check.</param>
    /// <returns>True if valid; otherwise false.</returns>
    public static bool IsValid(this SpectralSubtractor? value)
    {
        return value?.Validate().Count == 0;
    }

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
            $"SpectralSubtractor is invalid:{Environment.NewLine}  - {string.Join($"{Environment.NewLine}  - ", problems)}");
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

        // Check for null or empty
        if (noiseProfile.Length == 0)
        {
            problems.Add($"{paramName} must not be empty.");
        }

        // Check for NaN or infinity values
        for (int i = 0; i < noiseProfile.Length; i++)
        {
            if (double.IsNaN(noiseProfile[i]))
            {
                problems.Add($"{paramName}[{i}] must not be NaN.");
            }
            else if (double.IsInfinity(noiseProfile[i]))
            {
                problems.Add($"{paramName}[{i}] must not be infinite.");
            }
            else if (noiseProfile[i] < 0.0)
            {
                problems.Add($"{paramName}[{i}] must not be negative (got {noiseProfile[i].ToString(CultureInfo.InvariantCulture)}).");
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
    {
        return noiseProfile?.ValidateNoiseProfile().Count == 0;
    }

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
            $"Noise profile is invalid:{Environment.NewLine}  - {string.Join($"{Environment.NewLine}  - ", problems)}");
    }
}
