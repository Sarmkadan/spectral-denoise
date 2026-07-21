using System;
using System.Linq;
using SpectralDenoise;
using Xunit;

namespace SpectralDenoise.Tests
{
    public class SpectralSubtractorTests
    {
        [Fact]
        public void Process_ReduceResidualNoiseRms()
        {
            // Arrange
            var sampleRate = 44100;
            var frameSize = 1024;
            var hop = 256;
            var noiseProfile = new double[frameSize / 2 + 1];
            var subtractor = new SpectralSubtractor(frameSize, hop);
            var signal = GenerateSinePlusWhiteNoise(sampleRate, frameSize * 10);

            // Act
            var output = subtractor.Process(signal, noiseProfile);

            // Assert
            var inputRms = CalculateRms(signal);
            var outputRms = CalculateRms(output);
            Assert.True(outputRms < inputRms);
        }

        [Fact]
        public void Process_OutputLengthEqualsInputLength()
        {
            // Arrange
            var sampleRate = 44100;
            var frameSize = 1024;
            var hop = 256;
            var noiseProfile = new double[frameSize / 2 + 1];
            var subtractor = new SpectralSubtractor(frameSize, hop);
            var signal = GenerateSinePlusWhiteNoise(sampleRate, frameSize * 10);

            // Act
            var output = subtractor.Process(signal, noiseProfile);

            // Assert
            Assert.Equal(signal.Length, output.Length);
        }

        [Fact]
        public void Process_SilenceInSilenceOut()
        {
            // Arrange
            var sampleRate = 44100;
            var frameSize = 1024;
            var hop = 256;
            var noiseProfile = new double[frameSize / 2 + 1];
            var subtractor = new SpectralSubtractor(frameSize, hop);
            var signal = new float[frameSize * 10];

            // Act
            var output = subtractor.Process(signal, noiseProfile);

            // Assert
            Assert.All(output, x => Assert.Equal(0f, x));
        }

        [Fact]
        public void EstimateNoiseProfile_NoiseOnlyInput_ReturnsNonZeroProfile()
        {
            // Arrange
            var sampleRate = 44100;
            var frameSize = 1024;
            var hop = 256;
            var subtractor = new SpectralSubtractor(frameSize, hop);
            var noiseOnly = GenerateWhiteNoise(sampleRate, frameSize * 10);

            // Act
            var noiseProfile = subtractor.EstimateNoiseProfile(noiseOnly);

            // Assert
            Assert.All(noiseProfile, x => Assert.True(x > 0));
        }

        [Fact]
        public void EstimateNoiseProfile_SilenceInput_ReturnsZeroProfile()
        {
            // Arrange
            var sampleRate = 44100;
            var frameSize = 1024;
            var hop = 256;
            var subtractor = new SpectralSubtractor(frameSize, hop);
            var silence = new float[frameSize * 10];

            // Act
            var noiseProfile = subtractor.EstimateNoiseProfile(silence);

            // Assert
            Assert.All(noiseProfile, x => Assert.Equal(0, x));
        }

        private float[] GenerateSinePlusWhiteNoise(int sampleRate, int length)
        {
            var signal = new float[length];
            var random = new Random();
            for (int i = 0; i < length; i++)
            {
                signal[i] = (float)Math.Sin(2 * Math.PI * 1000 * i / sampleRate) + (float)(random.NextDouble() * 2 - 1);
            }
            return signal;
        }

        private float[] GenerateWhiteNoise(int sampleRate, int length)
        {
            var signal = new float[length];
            var random = new Random();
            for (int i = 0; i < length; i++)
            {
                signal[i] = (float)(random.NextDouble() * 2 - 1);
            }
            return signal;
        }

        private float CalculateRms(float[] signal)
        {
            return (float)Math.Sqrt(signal.Select(x => x * x).Average());
        }
    }
}
