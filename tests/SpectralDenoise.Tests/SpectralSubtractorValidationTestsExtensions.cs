using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SpectralDenoise.Tests
{
    /// <summary>
    /// Extension methods that make it easier to work with <see cref="SpectralSubtractorValidationTests"/>.
    /// </summary>
    public static class SpectralSubtractorValidationTestsExtensions
    {
        /// <summary>
        /// Executes every public validation, predicate and guard method on the supplied
        /// <see cref="SpectralSubtractorValidationTests"/> instance and returns any exceptions that were thrown.
        /// </summary>
        /// <param name="tests">The test instance to run.</param>
        /// <returns>An <see cref="IReadOnlyList{T}"/> containing the exceptions thrown by the individual methods.
        /// The list is empty when all methods succeed.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="tests"/> is <c>null</c>.</exception>
        public static IReadOnlyList<Exception> RunAllValidationsCollectingExceptions(this SpectralSubtractorValidationTests tests)
        {
            ArgumentNullException.ThrowIfNull(tests);

            var exceptions = new List<Exception>();

            foreach (var method in GetValidationMethods())
            {
                try
                {
                    method.Invoke(tests, null);
                }
                catch (TargetInvocationException tie) when (tie.InnerException is not null)
                {
                    exceptions.Add(tie.InnerException);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            return exceptions;
        }

        /// <summary>
        /// Executes every public validation, predicate and guard method on the supplied
        /// <see cref="SpectralSubtractorValidationTests"/> instance.
        /// </summary>
        /// <param name="tests">The test instance to run.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="tests"/> is <c>null</c>.</exception>
        /// <exception cref="AggregateException">
        /// Thrown when one or more of the invoked methods throw an exception.
        /// The inner exceptions contain the original failures.
        /// </exception>
        public static void RunAllValidations(this SpectralSubtractorValidationTests tests)
        {
            ArgumentNullException.ThrowIfNull(tests);

            var exceptions = tests.RunAllValidationsCollectingExceptions();

            if (exceptions.Count > 0)
            {
                throw new AggregateException(
                    "One or more SpectralSubtractor validation tests failed.",
                    exceptions);
            }
        }

        /// <summary>
        /// Returns the names of all public validation‑related methods defined on
        /// <see cref="SpectralSubtractorValidationTests"/>.
        /// </summary>
        /// <param name="tests">The test instance (only used for null checking).</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of method names.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="tests"/> is <c>null</c>.</exception>
        public static IEnumerable<string> GetValidationMethodNames(this SpectralSubtractorValidationTests tests)
        {
            ArgumentNullException.ThrowIfNull(tests);

            return GetValidationMethods()
                .Select(m => m.Name);
        }

        // Helper that discovers the relevant methods via reflection.
        private static IEnumerable<MethodInfo> GetValidationMethods()
        {
            return typeof(SpectralSubtractorValidationTests)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(m =>
                    m.Name.StartsWith("Validate_", StringComparison.Ordinal) ||
                    m.Name.StartsWith("IsValid_", StringComparison.Ordinal) ||
                    m.Name.StartsWith("EnsureValid_", StringComparison.Ordinal));
        }
    }
}
