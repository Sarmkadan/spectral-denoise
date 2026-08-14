using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SpectralDenoise
{
    public static class NoiseProfileValidation
    {
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

        public static bool IsValid(this NoiseProfile value)
        {
            ArgumentNullException.ThrowIfNull(value);

            return !Validate(value).Any();
        }

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
