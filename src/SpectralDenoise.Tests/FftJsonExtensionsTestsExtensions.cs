using System;
using System.Collections.Generic;
using System.Linq;

namespace SpectralDenoise.Tests
{
    /// <summary>
    /// Provides extension methods for <see cref="FftJsonExtensionsTests"/> to query and analyze test metadata.
    /// </summary>
    public static class FftJsonExtensionsTestsExtensions
    {
        private static readonly string[] AllTestNames = new[]
        {
            nameof(FftJsonExtensionsTests.ToJson_NullArray_ThrowsArgumentNullException),
            nameof(FftJsonExtensionsTests.ToJson_SimpleArray_ReturnsExpectedJson),
            nameof(FftJsonExtensionsTests.ToJson_Indented_ReturnsIndentedJson),
            nameof(FftJsonExtensionsTests.FromJson_NullOrEmpty_ThrowsArgumentException),
            nameof(FftJsonExtensionsTests.FromJson_ValidJson_ReturnsArray),
            nameof(FftJsonExtensionsTests.FromJson_InvalidJson_ReturnsNull),
            nameof(FftJsonExtensionsTests.TryFromJson_NullOrEmpty_ThrowsArgumentException),
            nameof(FftJsonExtensionsTests.TryFromJson_ValidJson_ReturnsTrueAndArray),
            nameof(FftJsonExtensionsTests.TryFromJson_InvalidJson_ReturnsFalseAndNull)
        };

        /// <summary>
        /// Gets the names of all test methods defined in <see cref="FftJsonExtensionsTests"/>.
        /// </summary>
        /// <param name="tests">The test class instance.</param>
        /// <returns>A read-only list of all test method names.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="tests"/> is null.</exception>
        public static IReadOnlyList<string> GetAllTestNames(this FftJsonExtensionsTests tests)
        {
            ArgumentNullException.ThrowIfNull(tests);
            return AllTestNames;
        }

        /// <summary>
        /// Gets the names of test methods that end with the specified suffix.
        /// </summary>
        /// <param name="tests">The test class instance.</param>
        /// <param name="suffix">The suffix to filter test method names by.</param>
        /// <returns>A read-only list of test method names matching the suffix.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="tests"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="suffix"/> is null or empty.</exception>
        public static IReadOnlyList<string> GetTestsBySuffix(this FftJsonExtensionsTests tests, string suffix)
        {
            ArgumentNullException.ThrowIfNull(tests);
            ArgumentException.ThrowIfNullOrEmpty(suffix);

            return tests.GetAllTestNames()
                .Where(name => name.EndsWith(suffix, StringComparison.Ordinal))
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Gets the names of test methods that are expected to throw exceptions based on naming convention.
        /// </summary>
        /// <param name="tests">The test class instance.</param>
        /// <returns>A read-only list of test method names that expect exceptions.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="tests"/> is null.</exception>
        public static IReadOnlyList<string> GetTestsExpectingExceptions(this FftJsonExtensionsTests tests) =>
            tests.GetTestsBySuffix("Throws");

        /// <summary>
        /// Gets the names of test methods that verify return values based on naming convention.
        /// </summary>
        /// <param name="tests">The test class instance.</param>
        /// <returns>A read-only list of test method names that verify return values.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="tests"/> is null.</exception>
        public static IReadOnlyList<string> GetTestsReturningData(this FftJsonExtensionsTests tests) =>
            tests.GetTestsBySuffix("Returns");
    }
}
