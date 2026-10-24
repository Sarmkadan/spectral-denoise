using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SpectralDenoise
{
    /// <summary>
    /// Validation helpers for <see cref="NoiseProfile"/>.
    /// </summary>
    public static class NoiseProfileValidation
    {
        /// <summary>
        /// Collects every validation problem found in the profile.
        /// </summary>
        /// <param name="value">The profile to validate.</param>
        /// <returns>A list of problem descriptions; empty when the profile is valid.</returns>
        public static IReadOnlyList<string> Validate(this NoiseProfile value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = new List<string>();

            if (value.Magnitudes == null || value.Magnitudes.Length == 0)
            {
                problems.Add("Magnitudes array is null or empty");
            }

            if (value.SampleRate <= 0)
            {
                problems.Add("Sample rate must be greater than zero");
            }

            if (value.FrameSize <= 0)
            {
                problems.Add("Frame size must be greater than zero");
            }

            if (value.Hop <= 0)
            {
                problems.Add("Hop size must be greater than zero");
            }

            return problems;
        }

        /// <summary>
        /// Checks whether the profile passes all validation rules.
        /// </summary>
        /// <param name="value">The profile to check.</param>
        /// <returns><see langword="true"/> when no problems are found; otherwise <see langword="false"/>.</returns>
        public static bool IsValid(this NoiseProfile value)
        {
            ArgumentNullException.ThrowIfNull(value);

            return !Validate(value).Any();
        }

        /// <summary>
        /// Throws if the profile fails any validation rule.
        /// </summary>
        /// <param name="value">The profile to check.</param>
        /// <exception cref="ArgumentException">Thrown when the profile has one or more validation problems.</exception>
        public static void EnsureValid(this NoiseProfile value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = Validate(value);

            if (problems.Any())
            {
                throw new ArgumentException($"Invalid NoiseProfile: {string.Join(", ", problems)}", nameof(value));
            }
        }
    }
}
