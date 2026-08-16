using Xunit;
using SpectralDenoise;

namespace SpectralDenoise.Tests
{
    public class WindowFunctionsTests : IWindowFunctionsTests
    {
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

        [Fact]
        public void HannWindow_EndpointsNearZero()
        {
            // Hann window should be exactly zero at endpoints
            var hann = WindowFunctions.Hann(100);
            Assert.InRange(hann[0], -0.01, 0.01);
            Assert.InRange(hann[hann.Length - 1], -0.01, 0.01);
        }

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

        [Fact]
        public void HannWindow_PeakAtCenter()
        {
            // Hann window should have peak at center for odd N
            var hann = WindowFunctions.Hann(101);
            int center = hann.Length / 2;
            Assert.Equal(1.0, hann[center]);
        }

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

        [Fact]
        public void HannWindow_SizeZero_ThrowsArgumentException()
        {
            // Requesting size 0 should throw ArgumentException
            var exception = Assert.Throws<ArgumentException>(() => WindowFunctions.Hann(0));
            Assert.Contains("size", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void HannWindow_NegativeSize_ThrowsArgumentException()
        {
            // Requesting negative size should throw ArgumentException
            var exception = Assert.Throws<ArgumentException>(() => WindowFunctions.Hann(-1));
            Assert.Contains("size", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void HannWindow_SizeOne_DoesNotThrow()
        {
            // Requesting size 1 should not throw (Hann's denominator is N-1, so size 1 is valid)
            var window = WindowFunctions.Hann(1);
            Assert.Single(window);
            Assert.True(double.IsNaN(window[0])); // Division by zero when period = 0
        }

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

        [Fact]
        public void HannPeriodic_EndpointsNearZero()
        {
            // HannPeriodic should also have endpoints near zero
            var hann = WindowFunctions.HannPeriodic(100);
            Assert.InRange(hann[0], -0.01, 0.01);
            Assert.InRange(hann[hann.Length - 1], -0.01, 0.01);
        }

        [Fact]
        public void NormalizeForCola_WithEmptyWindow_ThrowsArgumentException()
        {
            // Empty window should throw
            var exception = Assert.Throws<ArgumentException>(
                () => WindowFunctions.NormalizeForCola(ReadOnlySpan<double>.Empty, 1));
            Assert.Contains("empty", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

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

        [Fact]
        public void NormalizeForCola_ReturnsArrayOfSameLength()
        {
            var window = WindowFunctions.Hann(100);
            var normalized = WindowFunctions.NormalizeForCola(window, 50);
            Assert.Equal(window.Length, normalized.Length);
        }

        [Fact]
        public void SatisfiesCola_WithEmptyWindow_ReturnsFalse()
        {
            Assert.False(WindowFunctions.SatisfiesCola(ReadOnlySpan<double>.Empty, 1));
        }

        [Fact]
        public void SatisfiesCola_WithNonPositiveHop_ReturnsFalse()
        {
            var window = new double[] { 0.5, 0.5 };
            Assert.False(WindowFunctions.SatisfiesCola(window, 0));
            Assert.False(WindowFunctions.SatisfiesCola(window, -1));
        }
    }
}
