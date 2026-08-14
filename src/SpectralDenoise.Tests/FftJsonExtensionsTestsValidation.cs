using System;
using System.Collections.Generic;
using System.Linq;

namespace SpectralDenoise.Tests
{
    /// <summary>
    /// Provides validation helpers for <see cref="FftJsonExtensionsTests"/>.
    /// </summary>
    public static class FftJsonExtensionsTestsValidation
    {
        /// <summary>
        /// Validates the state of the supplied <see cref="FftJsonExtensionsTests"/> instance.
        /// </summary>
        /// <param name="value">The test instance to validate.</param>
        /// <returns>
        /// An <see cref="IReadOnlyList{T}"/> of human‑readable problem descriptions.
        /// The list is empty when the instance is considered valid.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
        public static IReadOnlyList<string> Validate(this FftJsonExtensionsTests value)
        {
            ArgumentNullException.ThrowIfNull(value);
            // The test class contains only methods; there is no mutable state to validate.
            // Returning an empty list indicates no validation problems.
            return Array.Empty<string>();
        }

        /// <summary>
        /// Determines whether the supplied <see cref="FftJsonExtensionsTests"/> instance is valid.
        /// </summary>
        /// <param name="value">The test instance to check.</param>
        /// <returns><c>true</c> if the instance has no validation problems; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
        public static bool IsValid(this FftJsonExtensionsTests value) =>
            !value.Validate().Any();

        /// <summary>
        /// Ensures that the supplied <see cref="FftJsonExtensionsTests"/> instance is valid.
        /// </summary>
        /// <param name="value">The test instance to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when one or more validation problems are found. The exception message contains a
        /// semicolon‑separated list of the problems.
        /// </exception>
        public static void EnsureValid(this FftJsonExtensionsTests value)
        {
            ArgumentNullException.ThrowIfNull(value);
            var problems = value.Validate();
            if (problems.Any())
            {
                throw new ArgumentException(string.Join("; ", problems), nameof(value));
            }
        }
    }
}
