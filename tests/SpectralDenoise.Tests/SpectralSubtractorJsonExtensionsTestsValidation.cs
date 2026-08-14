using System;
using System.Collections.Generic;
using System.Linq;

namespace SpectralDenoise.Tests
{
    /// <summary>
    /// Provides validation helpers for <see cref="SpectralSubtractorJsonExtensionsTests"/>.
    /// </summary>
    public static class SpectralSubtractorJsonExtensionsTestsValidation
    {
        /// <summary>
        /// Validates the supplied <see cref="SpectralSubtractorJsonExtensionsTests"/> instance.
        /// </summary>
        /// <param name="value">The test class instance to validate.</param>
        /// <returns>
        /// An <see cref="IReadOnlyList{T}"/> of human‑readable problem descriptions.
        /// The list is empty when the instance is considered valid.
        /// </returns>
        public static IReadOnlyList<string> Validate(this SpectralSubtractorJsonExtensionsTests? value)
        {
            if (value is null)
            {
                return new[] { "SpectralSubtractorJsonExtensionsTests instance is null." };
            }

            // The test class only contains methods; there are no data members to validate.
            // If future members are added, their validation should be added here.
            return Array.Empty<string>();
        }

        /// <summary>
        /// Determines whether the supplied <see cref="SpectralSubtractorJsonExtensionsTests"/> instance is valid.
        /// </summary>
        /// <param name="value">The test class instance to check.</param>
        /// <returns><c>true</c> if no validation problems are reported; otherwise, <c>false</c>.</returns>
        public static bool IsValid(this SpectralSubtractorJsonExtensionsTests? value) =>
            !value.Validate().Any();

        /// <summary>
        /// Ensures that the supplied <see cref="SpectralSubtractorJsonExtensionsTests"/> instance is valid.
        /// </summary>
        /// <param name="value">The test class instance to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when one or more validation problems are found. The exception message contains a
        /// semicolon‑separated list of the problems.
        /// </exception>
        public static void EnsureValid(this SpectralSubtractorJsonExtensionsTests? value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = value.Validate();
            if (problems.Count > 0)
            {
                throw new ArgumentException(string.Join("; ", problems), nameof(value));
            }
        }
    }
}
