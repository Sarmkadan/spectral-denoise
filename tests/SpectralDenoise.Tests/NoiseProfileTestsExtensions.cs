using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SpectralDenoise.Tests
{
    /// <summary>
    /// Extension methods that make it easier to work with <see cref="NoiseProfileTests"/>.
    /// </summary>
    public static class NoiseProfileTestsExtensions
    {
        /// <summary>
        /// Returns the names of all public instance test methods declared on <see cref="NoiseProfileTests"/>.
        /// </summary>
        /// <param name="tests">The test instance.</param>
        /// <returns>An <see cref="IReadOnlyList{T}"/> containing the method names.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tests"/> is <c>null</c>.</exception>
        public static IReadOnlyList<string> GetTestMethodNames(this NoiseProfileTests tests)
        {
            ArgumentNullException.ThrowIfNull(tests);

            // Test methods are public instance methods that return void and have no parameters.
            var methodNames = tests.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(m => m.ReturnType == typeof(void) && m.GetParameters().Length == 0)
                .Select(m => m.Name)
                .ToArray();

            return methodNames;
        }

        /// <summary>
        /// Executes all test methods on the supplied <see cref="NoiseProfileTests"/> instance.
        /// </summary>
        /// <param name="tests">The test instance.</param>
        /// <exception cref="ArgumentNullException"><paramref name="tests"/> is <c>null</c>.</exception>
        /// <exception cref="AggregateException">
        /// Thrown when one or more test methods throw an exception. The <see cref="AggregateException.InnerExceptions"/>
        /// collection contains the individual failures.
        /// </exception>
        public static void RunAll(this NoiseProfileTests tests)
        {
            ArgumentNullException.ThrowIfNull(tests);

            var exceptions = new List<Exception>();

            foreach (var method in tests.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(m => m.ReturnType == typeof(void) && m.GetParameters().Length == 0))
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

            if (exceptions.Count > 0)
                throw new AggregateException("One or more NoiseProfileTests failed.", exceptions);
        }

        /// <summary>
        /// Executes all test methods and returns any exceptions that were thrown.
        /// </summary>
        /// <param name="tests">The test instance.</param>
        /// <returns>
        /// An <see cref="IReadOnlyList{T}"/> of <see cref="Exception"/> objects.
        /// The list is empty when all tests pass.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="tests"/> is <c>null</c>.</exception>
        public static IReadOnlyList<Exception> RunAllCollectingExceptions(this NoiseProfileTests tests)
        {
            ArgumentNullException.ThrowIfNull(tests);

            var exceptions = new List<Exception>();

            foreach (var method in tests.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(m => m.ReturnType == typeof(void) && m.GetParameters().Length == 0))
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
        /// Determines whether all test methods on the supplied instance execute without throwing.
        /// </summary>
        /// <param name="tests">The test instance.</param>
        /// <returns><c>true</c> if every test method runs without exception; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tests"/> is <c>null</c>.</exception>
        public static bool AllTestsPass(this NoiseProfileTests tests)
        {
            ArgumentNullException.ThrowIfNull(tests);
            return tests.RunAllCollectingExceptions().Count == 0;
        }
    }
}
