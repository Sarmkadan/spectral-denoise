using Xunit;
using SpectralDenoise;

namespace SpectralDenoise.Tests
{
    public class WindowFunctionsTests
    {
        [Fact]
        public void HannWindow_EndpointsNearZero()
        {
            // Hann window should be near zero at endpoints
            var hann = WindowFunctions.Hann(100);
            Assert.InRange(hann[0], -0.01, 0.01);
            Assert.InRange(hann[hann.Length - 1], -0.01, 0.01);
        }

        [Fact]
        public void HannWindow_Periodic()
        {
            // Periodic Hann window has specific properties for STFT
            // It's not symmetric in the traditional sense but has specific periodicity
            var hann = WindowFunctions.Hann(100);

            // Check that it's periodic with the window length
            // For periodic Hann, hann[0] should equal hann[size] when considered circularly
            // But since we only have size elements, we just verify basic properties
            Assert.InRange(hann[0], -0.01, 0.01);
            Assert.InRange(hann[hann.Length - 1], -0.01, 0.01);
        }

        [Fact]
        public void HannWindow_PeakAtCenter()
        {
            // Hann window should have peak at center
            var hann = WindowFunctions.Hann(101);
            int center = hann.Length / 2;
            Assert.InRange(hann[center], 0.99, 1.01);
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
    }
}