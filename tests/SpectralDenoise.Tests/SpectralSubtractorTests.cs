using System;
using System.Linq;
using System.Numerics;
using System.Reflection;
using SpectralDenoise;
using Xunit;

namespace SpectralDenoise.Tests
{
    /// <summary>
    /// Test class for the SpectralSubtractor noise reduction algorithm.
    /// Verifies constructor behavior, property defaults, noise profile estimation,
    /// and audio processing under various conditions.
    /// </summary>
    public class SpectralSubtractorTests : ISpectralSubtractorTests
    {
        private const int SampleRate = 44100;
        private const int FrameSize = 1024;
        private const int Hop = 256;

        // Use periodic window for COLA compatibility
        private static readonly double[] Window = WindowFunctions.HannPeriodic(FrameSize);

        private static float[] GenerateSine(int length, double frequency = 1000.0)
        {
            var signal = new float[length];
            for (int i = 0; i < length; i++)
                signal[i] = (float)Math.Sin(2 * Math.PI * frequency * i / SampleRate);
            return signal;
        }

        private static float[] GenerateWhiteNoise(int length, int seed = 0)
        {
            var rnd = new Random(seed);
            var signal = new float[length];
            for (int i = 0; i < length; i++)
                signal[i] = (float)(rnd.NextDouble() * 2 - 1);
            return signal;
        }

        private static double Rms(double[] values) =>
            Math.Sqrt(values.Select(v => v * v).Average());

        private static double Rms(float[] values) =>
            Math.Sqrt(values.Select(v => (double)v * v).Average());

        /// <summary>
        /// Verifies that constructing a SpectralSubtractor with a non-power-of-two frame size throws an ArgumentException.
        /// </summary>
        [Fact]
        public void Constructor_NonPowerOfTwo_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new SpectralSubtractor(frameSize: 1000, hop: Hop));
        }

        /// <summary>
        /// Verifies that the default property values of a newly constructed SpectralSubtractor match the expected constants.
        /// </summary>
        [Fact]
        public void DefaultPropertyValues_AreAsExpected()
        {
            var sub = new SpectralSubtractor(FrameSize, Hop);
            Assert.Equal(2.0, sub.Alpha);
            Assert.Equal(0.02, sub.Beta);
            Assert.Equal(1.0, sub.OverSubtractionFactor);
            Assert.Equal(0.02, sub.SpectralFloor);
            Assert.Equal(DenoiseMode.SpectralSubtraction, sub.Mode);
            Assert.Equal(0.0, sub.AttackMs);
            Assert.Equal(0.0, sub.ReleaseMs);
        }

        /// <summary>
        /// Verifies that calling ResetSmoothing on a SpectralSubtractor instance does not throw any exceptions.
        /// </summary>
        [Fact]
        public void ResetSmoothing_DoesNotThrow()
        {
            var sub = new SpectralSubtractor(FrameSize, Hop);
            sub.ResetSmoothing(); // should simply clear internal state without exception
        }

        /// <summary>
        /// Verifies that EstimateNoiseProfile throws an InvalidOperationException when the input signal is shorter than one frame.
        /// </summary>
        [Fact]
        public void EstimateNoiseProfile_InsufficientLength_ThrowsInvalidOperationException()
        {
            var sub = new SpectralSubtractor(FrameSize, Hop);
            var shortSignal = new float[FrameSize / 2]; // shorter than one frame
            Assert.Throws<InvalidOperationException>(() => sub.EstimateNoiseProfile(shortSignal));
        }

        /// <summary>
        /// Verifies that EstimateNoiseProfile returns positive values for all frequency bins when given white noise input.
        /// </summary>
        [Fact]
        public void EstimateNoiseProfile_WhiteNoise_ReturnsPositiveValues()
        {
            var sub = new SpectralSubtractor(FrameSize, Hop);
            var noise = GenerateWhiteNoise(FrameSize * 5, seed: 42);
            var profile = sub.EstimateNoiseProfile(noise);
            Assert.All(profile, v => Assert.True(v > 0));
        }

        /// <summary>
        /// Verifies that EstimateNoiseProfile returns zeros for all frequency bins when given a silent input signal.
        /// </summary>
        [Fact]
        public void EstimateNoiseProfile_Silence_ReturnsAllZeros()
        {
            var sub = new SpectralSubtractor(FrameSize, Hop);
            var silence = new float[FrameSize * 5];
            var profile = sub.EstimateNoiseProfile(silence);
            Assert.All(profile, v => Assert.Equal(0.0, v, 12));
        }

        /// <summary>
        /// Verifies that Process throws an ArgumentException when the noise profile length does not match the expected FFT size.
        /// </summary>
        [Fact]
        public void Process_MismatchedNoiseProfile_ThrowsArgumentException()
        {
            var sub = new SpectralSubtractor(FrameSize, Hop);
            var signal = GenerateSine(FrameSize * 3);
            var wrongProfile = new double[10]; // wrong length
            Assert.Throws<ArgumentException>(() => sub.Process(signal, wrongProfile));
        }

        /// <summary>
        /// Verifies that Process returns an empty array when the input signal is empty.
        /// </summary>
        [Fact]
        public void Process_EmptySignal_ReturnsEmptyArray()
        {
            var sub = new SpectralSubtractor(FrameSize, Hop);
            var empty = Array.Empty<float>();
            var profile = new double[FrameSize / 2 + 1]; // zero noise profile
            var output = sub.Process(empty, profile);
            Assert.Empty(output);
        }

        /// <summary>
        /// Verifies that Process with OverSubtractionFactor set to zero and a zero noise profile leaves the signal unchanged (within numerical tolerance).
        /// </summary>
        [Fact]
        public void Process_OverSubtractionFactorZero_NoAttenuation()
        {
            // With OverSubtractionFactor = 0 and a zero noise profile,
            // the algorithm should leave the signal unchanged (within numerical tolerance).
            var sub = new SpectralSubtractor(FrameSize, Hop)
            {
                OverSubtractionFactor = 0.0
            };
            var signal = GenerateSine(FrameSize * 4);
            var zeroNoise = new double[FrameSize / 2 + 1]; // all zeros

            var output = sub.Process(signal, zeroNoise);

            // RMS of input and output should be almost identical
            double inputRms = Rms(signal);
            double outputRms = Rms(output);
            Assert.InRange(Math.Abs(outputRms - inputRms), 0, 1e-3);
        }

        /// <summary>
        /// Verifies that Process in Wiener mode reduces the overall RMS of a signal containing sine wave and additive white noise.
        /// </summary>
        [Fact]
        public void Process_WienerMode_ReducesNoise()
        {
            // Create a signal that is a sine plus white noise.
            var sub = new SpectralSubtractor(FrameSize, Hop)
            {
                Mode = DenoiseMode.Wiener,
                OverSubtractionFactor = 1.0,
                SpectralFloor = 0.0
            };
            var clean = GenerateSine(FrameSize * 4);
            var noise = GenerateWhiteNoise(FrameSize * 4, seed: 123);
            var mixed = clean.Zip(noise, (c, n) => c + n).ToArray();

            // Estimate noise profile from pure noise segment
            var noiseProfile = sub.EstimateNoiseProfile(noise);

            var output = sub.Process(mixed, noiseProfile);

            double inputRms = Rms(mixed);
            double outputRms = Rms(output);
            // Wiener filter should reduce RMS compared to the noisy input
            Assert.True(outputRms < inputRms);
        }

        /// <summary>
        /// Verifies that the default spectral floor (0.02) is inside [0, 1] and passes validation.
        /// </summary>
        [Fact]
        public void SpectralFloor_Default_IsValidAndPassesValidation()
        {
            var sub = new SpectralSubtractor(FrameSize, Hop);

            Assert.Equal(0.02, sub.SpectralFloor);
            Assert.True(sub.IsValid());
        }

        /// <summary>
        /// Verifies that SpectralFloor accepts the inclusive boundaries 0 and 1 and values just inside them.
        /// </summary>
        [Theory]
        [InlineData(0.0)]
        [InlineData(double.Epsilon)]
        [InlineData(0.5)]
        [InlineData(0.9999999999999999)] // Largest double below 1
        [InlineData(1.0)]
        public void SpectralFloor_ValueInsideRange_IsAccepted(double floor)
        {
            var sub = new SpectralSubtractor(FrameSize, Hop) { SpectralFloor = floor };

            Assert.Equal(floor, sub.SpectralFloor);
        }

        /// <summary>
        /// Verifies that SpectralFloor accepts the boundaries 0 and 1 in Wiener mode, where the floor still applies.
        /// </summary>
        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        public void SpectralFloor_WienerModeBoundary_IsAccepted(double floor)
        {
            var sub = new SpectralSubtractor(FrameSize, Hop) { Mode = DenoiseMode.Wiener, SpectralFloor = floor };

            Assert.Equal(floor, sub.SpectralFloor);
        }

        /// <summary>
        /// Verifies that SpectralFloor rejects values just outside [0, 1], values further outside, and non-finite values,
        /// and that the exception names the property and the valid range.
        /// </summary>
        [Theory]
        [InlineData(-double.Epsilon)]
        [InlineData(-1e-12)]
        [InlineData(-0.1)]
        [InlineData(1.0000000000000002)] // Smallest double above 1
        [InlineData(1.1)]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public void SpectralFloor_ValueOutsideRange_ThrowsNamingPropertyAndRange(double floor)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => new SpectralSubtractor(FrameSize, Hop) { SpectralFloor = floor });

            Assert.Equal("SpectralFloor", ex.ParamName);
            Assert.Contains("SpectralFloor", ex.Message);
            Assert.Contains("[0, 1]", ex.Message);
        }

        /// <summary>
        /// Verifies that SpectralFloor values outside [0, 1] are rejected in Wiener mode too.
        /// </summary>
        [Theory]
        [InlineData(-0.01)]
        [InlineData(1.01)]
        public void SpectralFloor_WienerModeOutOfRange_Throws(double floor)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new SpectralSubtractor(FrameSize, Hop) { Mode = DenoiseMode.Wiener, SpectralFloor = floor });
        }

        /// <summary>
        /// Verifies that a rejected assignment after construction leaves the previously valid spectral floor unchanged.
        /// </summary>
        [Fact]
        public void SpectralFloor_InvalidAssignmentAfterConstruction_KeepsPreviousValue()
        {
            var sub = new SpectralSubtractor(FrameSize, Hop) { SpectralFloor = 0.5 };

            Assert.Throws<ArgumentOutOfRangeException>(() => sub.SpectralFloor = 2.0);

            Assert.Equal(0.5, sub.SpectralFloor);
        }

        /// <summary>
        /// Verifies that Process does not re-validate SpectralFloor per frame. An out-of-range value written straight to the
        /// backing field (bypassing the setter) must be consumed across many frames without throwing; validation only runs on assignment.
        /// </summary>
        [Fact]
        public void Process_DoesNotRevalidateSpectralFloorPerFrame()
        {
            var sub = new SpectralSubtractor(FrameSize, Hop);
            var field = typeof(SpectralSubtractor).GetField("_spectralFloor", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            field!.SetValue(sub, 1.5);

            var noiseProfile = new double[FrameSize / 2 + 1];
            Array.Fill(noiseProfile, 0.01);
            var signal = GenerateSine(FrameSize * 16);

            var output = sub.Process(signal, noiseProfile);

            Assert.Equal(signal.Length, output.Length);
        }
    }
}
