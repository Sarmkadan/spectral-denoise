using System;
using System.Collections.Generic;
using System.Globalization;

namespace SpectralDenoise.Tests
{
    /// <summary>
    /// Provides validation helpers for <see cref="WavFileTests"/>.
    /// </summary>
    public static class WavFileTestsValidation
    {
        /// <summary>
        /// Validates the state of a <see cref="WavFileTests"/> instance.
        /// </summary>
        /// <param name="value">The instance to validate.</param>
        /// <returns>
        /// An <see cref="IReadOnlyList{T}"/> containing human‑readable problem descriptions.
        /// The list is empty when the instance is considered valid.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
        public static IReadOnlyList<string> Validate(this WavFileTests value)
        {
            ArgumentNullException.ThrowIfNull(value);

            // The test class does not expose any stateful members; validation is limited to null checks.
            var problems = new List<string>();
            return problems;
        }

        /// <summary>
        /// Determines whether a <see cref="WavFileTests"/> instance is valid.
        /// </summary>
        /// <param name="value">The instance to check.</param>
        /// <returns><c>true</c> if the instance has no validation problems; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
        public static bool IsValid(this WavFileTests value) => value.Validate().Count == 0;

        /// <summary>
        /// Ensures that a <see cref="WavFileTests"/> instance is valid.
        /// </summary>
        /// <param name="value">The instance to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when one or more validation problems are detected. The exception message contains a
        /// semicolon‑separated list of the problems.
        /// </exception>
        public static void EnsureValid(this WavFileTests value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = value.Validate();
            if (problems.Count > 0)
            {
                // Use invariant culture for deterministic formatting.
                var message = string.Join("; ", problems);
                throw new ArgumentException(message, nameof(value));
            }
        }
    }
}
