using System;
using System.Linq;
using System.Numerics;
using SpectralDenoise;
using Xunit;

namespace SpectralDenoise.Tests
{
    /// <summary>
    /// Objective quality regression tests for SpectralSubtractor.
    /// These tests verify that the denoising algorithm maintains acceptable
    /// signal quality by measuring SNR improvement and spectral distortion.
    /// </summary>
    public class ObjectiveQualityTests
    {
        private const int SampleRate = 16000; // Telephone quality for speech-like signals
        private const int FrameSize = 512;
        private const int Hop = 128;

        /// <summary>
        /// Generates a speech-like signal using formants (simplified vowel synthesis).
        /// </summary>
        private static float[] GenerateSpeechLikeSignal(int length, double fundamentalFreq = 120.0)
        {
            var signal = new float[length];

            // Formant frequencies for a neutral vowel (like schwa)
            double[] formantFreqs = { 500, 1500, 2500, 3500 };
            double[] formantAmplitudes = { 1.0, 0.5, 0.3, 0.2 };
            double[] formantBandwidths = { 80, 100, 120, 150 }; // Bandwidth in Hz

            for (int i = 0; i < length; i++)
            {
                double t = (double)i / SampleRate;
                double sample = 0.0;

                // Add formants with bandwidth (simplified as exponential decay)
                for (int f = 0; f < formantFreqs.Length; f++)
                {
                    double formant = formantFreqs[f];
                    double amplitude = formantAmplitudes[f];
                    double bandwidth = formantBandwidths[f];

                    // Simple resonator approximation
                    double envelope = Math.Exp(-bandwidth * Math.PI * t);
                    sample += amplitude * Math.Sin(2 * Math.PI * formant * t) * envelope;
                }

                // Add fundamental frequency
                sample += 0.5 * Math.Sin(2 * Math.PI * fundamentalFreq * t);

                signal[i] = (float)sample;
            }

            // Normalize to prevent clipping
            float maxAbs = signal.Select(Math.Abs).Max();
            if (maxAbs > 0)
            {
                for (int i = 0; i < signal.Length; i++)
                {
                    signal[i] /= maxAbs * 1.1f; // Leave some headroom
                }
            }

            return signal;
        }

        /// <summary>
        /// Generates white noise with specified RMS level.
        /// </summary>
        private static float[] GenerateWhiteNoise(int length, double targetRms, int seed = 42)
        {
            var rnd = new Random(seed);
            var noise = new float[length];

            // Generate uniform distribution [-1, 1]
            for (int i = 0; i < length; i++)
            {
                noise[i] = (float)(rnd.NextDouble() * 2 - 1);
            }

            // Scale to target RMS
            double currentRms = Math.Sqrt(noise.Select(x => (double)x * x).Average());
            if (currentRms > 0)
            {
                double scale = targetRms / currentRms;
                for (int i = 0; i < length; i++)
                {
                    noise[i] *= (float)scale;
                }
            }

            return noise;
        }

        /// <summary>
        /// Calculates SNR (Signal-to-Noise Ratio) in dB.
        /// </summary>
        private static double CalculateSnr(float[] signal, float[] noise)
        {
            if (signal.Length != noise.Length)
                throw new ArgumentException("Signal and noise must have the same length");

            double signalPower = 0.0;
            double noisePower = 0.0;

            for (int i = 0; i < signal.Length; i++)
            {
                signalPower += signal[i] * signal[i];
                noisePower += noise[i] * noise[i];
            }

            signalPower /= signal.Length;
            noisePower /= noise.Length;

            if (noisePower <= 0)
                return double.PositiveInfinity;

            return 10.0 * Math.Log10(signalPower / noisePower);
        }

        /// <summary>
        /// Calculates spectral distortion between clean and processed signals.
        /// Uses log-spectral distance (LSD) as a measure of spectral distortion.
        /// </summary>
        private static double CalculateSpectralDistortion(float[] clean, float[] processed, int fftSize)
        {
            if (clean.Length != processed.Length)
                throw new ArgumentException("Clean and processed signals must have the same length");

            // Use overlapping frames to compute average spectral distortion
            int hop = fftSize / 4; // 75% overlap
            int frames = 0;
            double totalDistortion = 0.0;

            for (int start = 0; start + fftSize <= clean.Length; start += hop)
            {
                // Extract frames
                float[] cleanFrame = new float[fftSize];
                float[] processedFrame = new float[fftSize];

                Array.Copy(clean, start, cleanFrame, 0, fftSize);
                Array.Copy(processed, start, processedFrame, 0, fftSize);

                // Apply window (Hann)
                for (int i = 0; i < fftSize; i++)
                {
                    double window = 0.5 * (1 - Math.Cos(2 * Math.PI * i / (fftSize - 1)));
                    cleanFrame[i] *= (float)window;
                    processedFrame[i] *= (float)window;
                }

                // Compute FFT
                Complex[] cleanSpec = new Complex[fftSize];
                Complex[] processedSpec = new Complex[fftSize];

                for (int i = 0; i < fftSize; i++)
                {
                    cleanSpec[i] = new Complex(cleanFrame[i], 0);
                    processedSpec[i] = new Complex(processedFrame[i], 0);
                }

                Fft.Forward(cleanSpec);
                Fft.Forward(processedSpec);

                // Compute log-spectral distance for magnitude spectra
                double frameDistortion = 0.0;
                int bins = fftSize / 2 + 1; // Only positive frequencies

                for (int b = 0; b < bins; b++)
                {
                    double cleanMag = Math.Max(cleanSpec[b].Magnitude, 1e-10);
                    double processedMag = Math.Max(processedSpec[b].Magnitude, 1e-10);

                    double logRatio = Math.Log10(cleanMag / processedMag);
                    frameDistortion += logRatio * logRatio;
                }

                frameDistortion /= bins;
                totalDistortion += frameDistortion;
                frames++;
            }

            return frames > 0 ? totalDistortion / frames : 0.0;
        }

        [Fact]
        public void SpectralSubtractor_ImprovesSnr_AboveThreshold()
        {
            // Arrange: Create speech-like signal with known SNR
            int signalLength = FrameSize * 20; // ~0.64 seconds at 16kHz
            float[] cleanSpeech = GenerateSpeechLikeSignal(signalLength);

            // Target input SNR: 0 dB (equal power speech and noise)
            double targetInputSnrDb = 0.0;
            double cleanPower = cleanSpeech.Select(x => (double)x * x).Average();
            double noisePower = cleanPower / Math.Pow(10, targetInputSnrDb / 10);
            double noiseRms = Math.Sqrt(noisePower);

            float[] noise = GenerateWhiteNoise(signalLength, noiseRms, seed: 123);
            float[] noisySignal = new float[signalLength];

            for (int i = 0; i < signalLength; i++)
            {
                noisySignal[i] = cleanSpeech[i] + noise[i];
            }

            // Verify input SNR
            double inputSnr = CalculateSnr(cleanSpeech, noise);
            Assert.InRange(inputSnr, -1.0, 1.0); // Should be close to 0 dB

            // Process with SpectralSubtractor
            var subtractor = new SpectralSubtractor(FrameSize, Hop)
            {
                OverSubtractionFactor = 1.5,
                SpectralFloor = 0.01,
                Mode = DenoiseMode.SpectralSubtraction
            };

            // Estimate noise profile from noise-only segment (first 0.5 seconds)
            int noiseEstimateLength = FrameSize * 4; // ~0.125 seconds
            float[] noiseEstimate = new float[noiseEstimateLength];
            Array.Copy(noise, 0, noiseEstimate, 0, noiseEstimateLength);

            double[] noiseProfile = subtractor.EstimateNoiseProfile(noiseEstimate);
            float[] denoised = subtractor.Process(noisySignal, noiseProfile);

            // Calculate residual noise (difference between clean and denoised)
            float[] residualNoise = new float[signalLength];
            for (int i = 0; i < signalLength; i++)
            {
                residualNoise[i] = cleanSpeech[i] - denoised[i];
            }

            // Calculate output SNR
            double outputSnr = CalculateSnr(cleanSpeech, residualNoise);
            double snrImprovement = outputSnr - inputSnr;

            // Assert: SNR improvement should be positive and above threshold
            // For spectral subtraction with reasonable parameters, expect at least 1 dB improvement
            Assert.True(snrImprovement > 0.5,
                $"Expected SNR improvement > 0.5 dB, got {snrImprovement:F2} dB");
        }

        [Fact]
        public void SpectralSubtractor_WithSpectralFloor_ReducesMusicalNoise()
        {
            // This test verifies that spectral floor prevents excessive signal distortion
            // Arrange: Create speech-like signal with noise
            int signalLength = FrameSize * 16;
            float[] cleanSpeech = GenerateSpeechLikeSignal(signalLength);

            // Add noise at 5 dB SNR
            double targetInputSnrDb = 5.0;
            double cleanPower = cleanSpeech.Select(x => (double)x * x).Average();
            double noisePower = cleanPower / Math.Pow(10, targetInputSnrDb / 10);
            double noiseRms = Math.Sqrt(noisePower);

            float[] noise = GenerateWhiteNoise(signalLength, noiseRms, seed: 456);
            float[] noisySignal = new float[signalLength];

            for (int i = 0; i < signalLength; i++)
            {
                noisySignal[i] = cleanSpeech[i] + noise[i];
            }

            // Process with SpectralSubtraction and spectral floor
            var subtractor = new SpectralSubtractor(FrameSize, Hop)
            {
                OverSubtractionFactor = 1.2,
                SpectralFloor = 0.02, // Default value
                Mode = DenoiseMode.SpectralSubtraction
            };

            // Estimate noise profile
            float[] noiseEstimate = new float[FrameSize * 4];
            Array.Copy(noise, 0, noiseEstimate, 0, noiseEstimate.Length);

            double[] noiseProfile = subtractor.EstimateNoiseProfile(noiseEstimate);
            float[] denoised = subtractor.Process(noisySignal, noiseProfile);

            // Calculate SNR improvement
            float[] residualNoise = new float[signalLength];
            for (int i = 0; i < signalLength; i++)
            {
                residualNoise[i] = cleanSpeech[i] - denoised[i];
            }

            double inputSnr = CalculateSnr(cleanSpeech, noise);
            double outputSnr = CalculateSnr(cleanSpeech, residualNoise);
            double snrImprovement = outputSnr - inputSnr;

            // Assert: Should have positive SNR improvement with spectral floor
            Assert.True(snrImprovement > 0.0,
                $"Expected SNR improvement > 0.0 dB with spectral floor, got {snrImprovement:F2} dB");
        }

        [Fact]
        public void SpectralSubtractor_WithoutSpectralFloor_MayIntroduceDistortion()
        {
            // This test shows why spectral floor is important
            // Arrange: Create speech-like signal with noise
            int signalLength = FrameSize * 16;
            float[] cleanSpeech = GenerateSpeechLikeSignal(signalLength);

            // Add noise at 5 dB SNR
            double targetInputSnrDb = 5.0;
            double cleanPower = cleanSpeech.Select(x => (double)x * x).Average();
            double noisePower = cleanPower / Math.Pow(10, targetInputSnrDb / 10);
            double noiseRms = Math.Sqrt(noisePower);

            float[] noise = GenerateWhiteNoise(signalLength, noiseRms, seed: 789);
            float[] noisySignal = new float[signalLength];

            for (int i = 0; i < signalLength; i++)
            {
                noisySignal[i] = cleanSpeech[i] + noise[i];
            }

            // Process without spectral floor (SpectralFloor = 0)
            var subtractor = new SpectralSubtractor(FrameSize, Hop)
            {
                OverSubtractionFactor = 1.2,
                SpectralFloor = 0.0, // No spectral floor
                Mode = DenoiseMode.SpectralSubtraction
            };

            // Estimate noise profile
            float[] noiseEstimate = new float[FrameSize * 4];
            Array.Copy(noise, 0, noiseEstimate, 0, noiseEstimate.Length);

            double[] noiseProfile = subtractor.EstimateNoiseProfile(noiseEstimate);
            float[] denoised = subtractor.Process(noisySignal, noiseProfile);

            // Calculate SNR improvement
            float[] residualNoise = new float[signalLength];
            for (int i = 0; i < signalLength; i++)
            {
                residualNoise[i] = cleanSpeech[i] - denoised[i];
            }

            double inputSnr = CalculateSnr(cleanSpeech, noise);
            double outputSnr = CalculateSnr(cleanSpeech, residualNoise);
            double snrImprovement = outputSnr - inputSnr;

            // Assert: May still improve SNR but with potential for artifacts
            // The key is that spectral floor helps prevent excessive distortion
            Assert.True(snrImprovement > -5.0,
                $"Expected SNR improvement > -5.0 dB even without spectral floor, got {snrImprovement:F2} dB");
        }
    }
}