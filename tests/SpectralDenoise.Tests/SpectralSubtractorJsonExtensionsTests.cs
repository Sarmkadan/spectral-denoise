using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using SpectralDenoise;
using Xunit;

namespace SpectralDenoise.Tests
{
    public class SpectralSubtractorJsonExtensionsTests
    {
        [Fact]
        public void ToJson_HappyPath_ReturnsJsonString()
        {
            // Arrange
            var spectralSubtractor = new SpectralSubtractor(1024, 256);
            var expectedJson = "{\"Alpha\":2.0,\"Beta\":0.02,\"Mode\":\"SpectralSubtraction\",\"OverSubtractionFactor\":1.0,\"SpectralFloor\":0.02,\"AttackMs\":0.0,\"ReleaseMs\":0.0}";

            // Act
            var actualJson = spectralSubtractor.ToJson();

            // Assert
            Assert.Equal(expectedJson, actualJson);
        }

        [Fact]
        public void ToJson_NullInput_ThrowsArgumentNullException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => ((SpectralSubtractor)null).ToJson());
        }

        [Fact]
        public void FromJson_HappyPath_ReturnsSpectralSubtractor()
        {
            // Arrange
            var json = "{\"Alpha\":2.0,\"Beta\":0.02,\"Mode\":\"SpectralSubtraction\",\"OverSubtractionFactor\":1.0,\"SpectralFloor\":0.02,\"AttackMs\":0.0,\"ReleaseMs\":0.0}";

            // Act
            var actualSpectralSubtractor = SpectralSubtractorJsonExtensions.FromJson(json);

            // Assert
            Assert.NotNull(actualSpectralSubtractor);
        }

        [Fact]
        public void FromJson_NullInput_ReturnsNull()
        {
            // Act
            var actualSpectralSubtractor = SpectralSubtractorJsonExtensions.FromJson(null);

            // Assert
            Assert.Null(actualSpectralSubtractor);
        }

        [Fact]
        public void FromJson_EmptyJson_ReturnsNull()
        {
            // Act
            var actualSpectralSubtractor = SpectralSubtractorJsonExtensions.FromJson("");

            // Assert
            Assert.Null(actualSpectralSubtractor);
        }

        [Fact]
        public void TryFromJson_HappyPath_ReturnsTrue()
        {
            // Arrange
            var json = "{\"Alpha\":2.0,\"Beta\":0.02,\"Mode\":\"SpectralSubtraction\",\"OverSubtractionFactor\":1.0,\"SpectralFloor\":0.02,\"AttackMs\":0.0,\"ReleaseMs\":0.0}";

            // Act
            var actualResult = SpectralSubtractorJsonExtensions.TryFromJson(json, out var actualSpectralSubtractor);

            // Assert
            Assert.True(actualResult);
            Assert.NotNull(actualSpectralSubtractor);
        }

        [Fact]
        public void TryFromJson_NullInput_ReturnsFalse()
        {
            // Act
            var actualResult = SpectralSubtractorJsonExtensions.TryFromJson(null, out var actualSpectralSubtractor);

            // Assert
            Assert.False(actualResult);
            Assert.Null(actualSpectralSubtractor);
        }

        [Fact]
        public void TryFromJson_EmptyJson_ReturnsFalse()
        {
            // Act
            var actualResult = SpectralSubtractorJsonExtensions.TryFromJson("", out var actualSpectralSubtractor);

            // Assert
            Assert.False(actualResult);
            Assert.Null(actualSpectralSubtractor);
        }
    }
}
