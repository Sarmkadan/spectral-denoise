using System;
using System.Linq;
using System.Numerics;
using SpectralDenoise;
using Xunit;

namespace SpectralDenoise.Tests
{
    public class SpectralSubtractorTests
    {
        private const int SampleRate = 44100;
        private const int FrameSize = 1024;
        private const int Hop = 256;

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

        [Fact]
        public void Constructor_NonPowerOfTwo_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new SpectralSubtractor(frameSize: 1000, hop: Hop));
        }

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

        [Fact]
        public void ResetSmoothing_DoesNotThrow()
        {
            var sub = new SpectralSubtractor(FrameSize, Hop);
            sub.ResetSmoothing(); // should simply clear internal state without exception
        }

        [Fact]
        public void EstimateNoiseProfile_InsufficientLength_ThrowsInvalidOperationException()
        {
            var sub = new SpectralSubtractor(FrameSize, Hop);
            var shortSignal = new float[FrameSize / 2]; // shorter than one frame
            Assert.Throws<InvalidOperationException>(() => sub.EstimateNoiseProfile(shortSignal));
        }

        [Fact]
        public void EstimateNoiseProfile_WhiteNoise_ReturnsPositiveValues()
        {
            var sub = new SpectralSubtractor(FrameSize, Hop);
            var noise = GenerateWhiteNoise(FrameSize * 5, seed: 42);
            var profile = sub.EstimateNoiseProfile(noise);
            Assert.All(profile, v => Assert.True(v > 0));
        }

        [Fact]
        public void EstimateNoiseProfile_Silence_ReturnsAllZeros()
        {
            var sub = new SpectralSubtractor(FrameSize, Hop);
            var silence = new float[FrameSize * 5];
            var profile = sub.EstimateNoiseProfile(silence);
            Assert.All(profile, v => Assert.Equal(0.0, v, 12));
        }

        [Fact]
        public void Process_MismatchedNoiseProfile_ThrowsArgumentException()
        {
            var sub = new SpectralSubtractor(FrameSize, Hop);
            var signal = GenerateSine(FrameSize * 3);
            var wrongProfile = new double[10]; // wrong length
            Assert.Throws<ArgumentException>(() => sub.Process(signal, wrongProfile));
        }

        [Fact]
        public void Process_EmptySignal_ReturnsEmptyArray()
        {
            var sub = new SpectralSubtractor(FrameSize, Hop);
            var empty = Array.Empty<float>();
            var profile = new double[FrameSize / 2 + 1]; // zero noise profile
            var output = sub.Process(empty, profile);
            Assert.Empty(output);
        }

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
    }
}
