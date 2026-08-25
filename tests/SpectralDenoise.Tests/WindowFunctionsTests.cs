using Xunit;
using SpectralDenoise;

namespace SpectralDenoise.Tests
{
    /// <summary>
    /// Unit tests for the Hann window factories and the constant-overlap-add (COLA)
    /// helper methods provided by the WindowFunctions class.
    /// </summary>
    public class WindowFunctionsTests : IWindowFunctionsTests
    {
        /// <summary>
        /// Verifies that Hann(size) returns an array containing exactly 'size'
        /// elements for every requested size from 1 through 100.
        /// </summary>
        [Fact]
        public void HannWindow_ReturnsArrayOfCorrectLength()
        {
            // Test that requesting a window of size N returns an array of exactly length N
            for (int size = 1; size <= 100; size++)
            {
                var window = WindowFunctions.Hann(size);
                Assert.Equal(size, window.Length);
            }
        }

        /// <summary>
        /// Verifies that the periodic variant of Hann(size) also returns an array
        /// containing exactly 'size' elements for every requested size from 1 through 100.
        /// </summary>
        [Fact]
        public void HannWindow_ReturnsArrayOfCorrectLength_WithPeriodic()
        {
            // Test periodic variant also returns correct length
            for (int size = 1; size <= 100; size++)
            {
                var window = WindowFunctions.Hann(size, periodic: true);
                Assert.Equal(size, window.Length);
            }
        }

        /// <summary>
        /// Verifies that the first and last samples of a 100-point symmetric Hann
        /// window are approximately zero.
        /// </summary>
        [Fact]
        public void HannWindow_EndpointsNearZero()
        {
            // Hann window should be exactly zero at endpoints
            var hann = WindowFunctions.Hann(100);
            Assert.InRange(hann[0], -0.01, 0.01);
            Assert.InRange(hann[hann.Length - 1], -0.01, 0.01);
        }

        /// <summary>
        /// Verifies that the periodic Hann variant produces arrays of the exact
        /// requested length for every size from 1 through 100.
        /// </summary>
        [Fact]
        public void HannWindow_Periodic()
        {
            // Test periodic variant also returns correct length
            for (int size = 1; size <= 100; size++)
            {
                var window = WindowFunctions.Hann(size, periodic: true);
                Assert.Equal(size, window.Length);
            }
        }

        /// <summary>
        /// Verifies that a 101-point symmetric Hann window attains its maximum
        /// value of exactly 1.0 at its center sample.
        /// </summary>
        [Fact]
        public void HannWindow_PeakAtCenter()
        {
            // Hann window should have peak at center for odd N
            var hann = WindowFunctions.Hann(101);
            int center = hann.Length / 2;
            Assert.Equal(1.0, hann[center]);
        }

        /// <summary>
        /// Verifies that every sample of a 100-point Hann window lies within the
        /// inclusive range [0, 1].
        /// </summary>
        [Fact]
        public void HannWindow_AllValuesBetweenZeroAndOne()
        {
            // All Hann window values should be between 0 and 1
            var hann = WindowFunctions.Hann(100);
            foreach (var value in hann)
            {
                Assert.InRange(value, 0.0, 1.0);
            }
        }

        /// <summary>
        /// Verifies that a 101-point Hann window is mirror-symmetric about its
        /// center by comparing each sample against its mirrored counterpart.
        /// </summary>
        [Fact]
        public void HannWindow_IsSymmetric()
        {
            // Hann window should be symmetric around its center
            var hann = WindowFunctions.Hann(101);
            int center = hann.Length / 2;

            for (int i = 0; i < center; i++)
            {
                int mirrorIndex = hann.Length - 1 - i;
                Assert.Equal(hann[i], hann[mirrorIndex], 14);
            }
        }

        /// <summary>
        /// Verifies that requesting a zero-length Hann window throws an
        /// ArgumentException whose message references the size argument.
        /// </summary>
        [Fact]
        public void HannWindow_SizeZero_ThrowsArgumentException()
        {
            // Requesting size 0 should throw ArgumentException
            var exception = Assert.Throws<ArgumentException>(() => WindowFunctions.Hann(0));
            Assert.Contains("size", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies that requesting a negative-size Hann window throws an
        /// ArgumentException whose message references the size argument.
        /// </summary>
        [Fact]
        public void HannWindow_NegativeSize_ThrowsArgumentException()
        {
            // Requesting negative size should throw ArgumentException
            var exception = Assert.Throws<ArgumentException>(() => WindowFunctions.Hann(-1));
            Assert.Contains("size", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies that a single-sample Hann window request succeeds, returning
        /// one element whose value is NaN because the N-1 denominator evaluates to zero.
        /// </summary>
        [Fact]
        public void HannWindow_SizeOne_DoesNotThrow()
        {
            // Requesting size 1 should not throw (Hann's denominator is N-1, so size 1 is valid)
            var window = WindowFunctions.Hann(1);
            Assert.Single(window);
            Assert.True(double.IsNaN(window[0])); // Division by zero when period = 0
        }

        /// <summary>
        /// Verifies that the periodic and symmetric 100-point Hann windows differ
        /// in at least one sample value.
        /// </summary>
        [Fact]
        public void HannWindow_PeriodicVariant_HasDifferentValues()
        {
            // Periodic variant should produce different values than symmetric variant
            var symmetric = WindowFunctions.Hann(100, periodic: false);
            var periodic = WindowFunctions.Hann(100, periodic: true);

            // They should be different for most indices
            int differentCount = 0;
            for (int i = 0; i < symmetric.Length; i++)
            {
                if (Math.Abs(symmetric[i] - periodic[i]) > 1e-10)
                {
                    differentCount++;
                }
            }

            // At least some values should be different
            Assert.True(differentCount > 0);
        }

        /// <summary>
        /// Verifies that HannPeriodic(size) returns an array containing exactly
        /// 'size' elements for every requested size from 4 through 100.
        /// </summary>
        [Fact]
        public void HannPeriodic_ReturnsArrayOfCorrectLength()
        {
            // Test HannPeriodic returns correct length
            for (int size = 4; size <= 100; size++)
            {
                var window = WindowFunctions.HannPeriodic(size);
                Assert.Equal(size, window.Length);
            }
        }

        /// <summary>
        /// Verifies that the first and last samples of a 100-point periodic Hann
        /// window are approximately zero.
        /// </summary>
        [Fact]
        public void HannPeriodic_EndpointsNearZero()
        {
            // HannPeriodic should also have endpoints near zero
            var hann = WindowFunctions.HannPeriodic(100);
            Assert.InRange(hann[0], -0.01, 0.01);
            Assert.InRange(hann[hann.Length - 1], -0.01, 0.01);
        }

        /// <summary>
        /// Verifies that normalizing an empty window span throws an ArgumentException
        /// whose message indicates the input is empty.
        /// </summary>
        [Fact]
        public void NormalizeForCola_WithEmptyWindow_ThrowsArgumentException()
        {
            // Empty window should throw
            var exception = Assert.Throws<ArgumentException>(
                () => WindowFunctions.NormalizeForCola(ReadOnlySpan<double>.Empty, 1));
            Assert.Contains("empty", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies that normalizing with a hop of zero and with a negative hop
        /// both throw an ArgumentException whose message requires a positive hop.
        /// </summary>
        [Fact]
        public void NormalizeForCola_WithNonPositiveHop_ThrowsArgumentException()
        {
            var window = new double[] { 0.5, 0.5 };

            var exception1 = Assert.Throws<ArgumentException>(
                () => WindowFunctions.NormalizeForCola(window, 0));
            Assert.Contains("positive", exception1.Message, StringComparison.OrdinalIgnoreCase);

            var exception2 = Assert.Throws<ArgumentException>(
                () => WindowFunctions.NormalizeForCola(window, -1));
            Assert.Contains("positive", exception2.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies that normalizing a 100-point Hann window with a hop of 50
        /// returns a result having the same length as the input window.
        /// </summary>
        [Fact]
        public void NormalizeForCola_ReturnsArrayOfSameLength()
        {
            var window = WindowFunctions.Hann(100);
            var normalized = WindowFunctions.NormalizeForCola(window, 50);
            Assert.Equal(window.Length, normalized.Length);
        }

        /// <summary>
        /// Verifies that the COLA check reports false when given an empty window span.
        /// </summary>
        [Fact]
        public void SatisfiesCola_WithEmptyWindow_ReturnsFalse()
        {
            Assert.False(WindowFunctions.SatisfiesCola(ReadOnlySpan<double>.Empty, 1));
        }

        /// <summary>
        /// Verifies that the COLA check reports false for a hop of zero and for
        /// a negative hop.
        /// </summary>
        [Fact]
        public void SatisfiesCola_WithNonPositiveHop_ReturnsFalse()
        {
            var window = new double[] { 0.5, 0.5 };
            Assert.False(WindowFunctions.SatisfiesCola(window, 0));
            Assert.False(WindowFunctions.SatisfiesCola(window, -1));
        }
    }
}
