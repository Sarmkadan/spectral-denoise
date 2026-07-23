using System;
using System.Linq;
using SpectralDenoise;
using Xunit;

namespace SpectralDenoise.Tests
{
    public class SpectralSubtractorExtensionsTests
    {
        private const int SampleRate = 44100;
        private const int FrameSize = 8;   // power‑of‑two, small for fast tests
        private const int Hop = 4;

        private static float[] CreateSignal(float value) =>
            Enumerable.Repeat(value, FrameSize * 2).ToArray();

        private static double[] ZeroNoiseProfile => new double[FrameSize / 2 + 1];

        [Fact]
        public void Process_ReturnsSameSignal_WhenOverSubtractionZeroAndZeroNoise()
        {
            var sub = new SpectralSubtractor(FrameSize, Hop) { OverSubtractionFactor = 0.0 };
            var signal = new float[] { 0.1f, -0.2f, 0.3f, -0.4f, 0.5f, -0.6f, 0.7f, -0.8f };
            var output = new float[signal.Length];

            var result = sub.Process(signal, ZeroNoiseProfile, output);

            Assert.Equal(signal.Length, result.Length);
            for (int i = 0; i < signal.Length; i++)
                Assert.InRange(Math.Abs(result[i] - signal[i]), 0, 1e-6);
        }

        [Fact]
        public void Process_ThrowsArgumentException_WhenSignalIsEmpty()
        {
            var sub = new SpectralSubtractor(FrameSize, Hop);
            var output = new float[1];
            Assert.Throws<ArgumentException>(() => sub.Process(ReadOnlySpan<float>.Empty, ZeroNoiseProfile, output));
        }

        [Fact]
        public void EstimateNormalizedNoiseProfile_NormalizesRms_ToTarget()
        {
            var sub = new SpectralSubtractor(FrameSize, Hop);
            var noise = Enumerable.Repeat(1.0f, FrameSize * 2).ToArray();

            var profile = sub.EstimateNormalizedNoiseProfile(noise, normalize: true);

            double rms = Math.Sqrt(profile.Sum(p => p * p) / profile.Length);
            Assert.InRange(rms, 0.099, 0.101); // target RMS is 0.1
        }

        [Fact]
        public void EstimateNormalizedNoiseProfile_LeavesZeroProfile_Unaffected()
        {
            var sub = new SpectralSubtractor(FrameSize, Hop);
            var silence = new float[FrameSize * 2]; // all zeros

            var profile = sub.EstimateNormalizedNoiseProfile(silence, normalize: true);

            Assert.All(profile, v => Assert.Equal(0.0, v, 12));
        }

        [Fact]
        public void ProcessWithSilenceDetection_SkipsSilentFrames()
        {
            var sub = new SpectralSubtractor(FrameSize, Hop) { OverSubtractionFactor = 0.0 };
            var signal = new float[FrameSize * 2];
            // first frame silent (zeros), second frame non‑silent (0.5)
            for (int i = FrameSize; i < signal.Length; i++) signal[i] = 0.5f;

            var output = sub.ProcessWithSilenceDetection(signal, ZeroNoiseProfile, silenceThreshold: 0.01f);

            // first half should be zero
            for (int i = 0; i < FrameSize; i++)
                Assert.Equal(0.0f, output[i], 6);

            // second half should match the input (within tolerance)
            for (int i = FrameSize; i < output.Length; i++)
                Assert.InRange(Math.Abs(output[i] - signal[i]), 0, 1e-6);
        }

        [Fact]
        public void GetFrameSizeAndHopSize_ReturnCorrectValues()
        {
            var sub = new SpectralSubtractor(FrameSize, Hop);
            Assert.Equal(FrameSize, sub.GetFrameSize());
            Assert.Equal(Hop, sub.GetHopSize());
        }

        [Fact]
        public void GetWindow_ReturnsCopy_NotReference()
        {
            var sub = new SpectralSubtractor(FrameSize, Hop);
            var window1 = sub.GetWindow();
            var window2 = sub.GetWindow();

            Assert.NotSame(window1, window2);
            // mutate first copy and ensure second copy is unchanged
            window1[0] = -1.0;
            Assert.NotEqual(window1[0], window2[0]);
        }
    }
}
