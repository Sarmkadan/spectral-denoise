using Xunit;
using SpectralDenoise;
using System.Numerics;

namespace SpectralDenoise.Tests
{
    /// <summary>
/// Unit tests for the Fast Fourier Transform (FFT) functionality in the SpectralDenoise library.
/// Tests verify the correctness of forward and inverse FFT operations, including edge cases
/// and specific frequency bin behaviors.
/// </summary>
public class FftTests
    {
        /// <summary>
        /// Tests that a real sine wave at an exact frequency bin produces a peak at that bin in the FFT output.
        /// Creates a sine wave with frequency matching a specific FFT bin, computes the FFT, and verifies
        /// that the magnitude at the expected bin is significantly larger than the theoretical minimum.
        /// </summary>
        [Fact]
        public void Fft_RealSineAtExactBin_GivesPeakAtThatBin()
        {
            // Create a sine wave at exactly one bin frequency
            // For a signal of length N, a sine at frequency k has period N/k samples
            int length = 1024;
            int bin = 5; // 5th bin
            double frequency = (double)bin / length;

            // Generate sine wave at exact bin frequency
            Complex[] signal = new Complex[length];
            for (int i = 0; i < length; i++)
            {
                signal[i] = new Complex(Math.Sin(2 * Math.PI * bin * i / length), 0);
            }

            // Compute FFT
            Fft.Forward(signal);

            // The peak should be at the bin frequency
            // Due to FFT symmetry, we check both positive and negative frequencies
            int peakBin = bin;
            double magnitude = signal[peakBin].Magnitude;
            Assert.True(magnitude > 0.9 * length / 2.0, $"Expected peak at bin {peakBin} with magnitude > {0.9 * length / 2.0}, got {magnitude}");
        }

        /// <summary>
        /// Tests that a zero signal produces a zero spectrum after FFT transformation.
        /// Verifies that the FFT of a signal containing only zeros results in all frequency components
        /// having magnitudes close to zero, confirming proper handling of edge cases.
        /// </summary>
        [Fact]
        public void Fft_ZeroSignal_ReturnsAllZeros()
        {
            // Zero signal should produce zero spectrum
            int length = 64;
            Complex[] signal = new Complex[length];

            Fft.Forward(signal);

            foreach (var value in signal)
            {
                Assert.Equal(0.0, value.Magnitude, 10);
            }
        }

        /// <summary>
        /// Tests that applying forward FFT followed by inverse FFT returns the original signal.
        /// Verifies the invertibility property of the FFT algorithm by generating a random signal,
        /// applying FFT, then inverse FFT, and checking that the reconstructed signal matches
        /// the original within a small tolerance.
        /// </summary>
        [Fact]
        public void Fft_ForwardInverse_ReturnsOriginalSignal()
        {
            // Forward then inverse FFT should return original signal
            int length = 128;
            Complex[] original = new Complex[length];
            var random = new Random(42);
            for (int i = 0; i < length; i++)
            {
                original[i] = new Complex(random.NextDouble() * 2 - 1, 0);
            }

            Complex[] workingCopy = new Complex[length];
            Array.Copy(original, workingCopy, length);

            Fft.Forward(workingCopy);
            Fft.Inverse(workingCopy);

            // Check that reconstructed signal matches original
            for (int i = 0; i < length; i++)
            {
                Assert.Equal(original[i].Real, workingCopy[i].Real, 5);
            }
        }
    }
}