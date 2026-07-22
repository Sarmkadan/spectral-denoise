using System;
using SpectralDenoise;
using Xunit;

namespace SpectralDenoise.Tests
{
    public class SpectralSubtractorValidationTests
    {
        [Fact]
        public void Validate_SpectralSubtractorWithValidProperties_ReturnsEmptyList()
        {
            // Arrange
            var subtractor = new SpectralSubtractor(1024, 256)
            {
                Alpha = 2.0,
                SpectralFloor = 0.02
            };

            // Act
            var result = subtractor.Validate();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void Validate_SpectralSubtractorWithAlphaLessThanOne_ReturnsError()
        {
            // Arrange
            var subtractor = new SpectralSubtractor(1024, 256)
            {
                Alpha = 0.5,
                SpectralFloor = 0.02
            };

            // Act
            var result = subtractor.Validate();

            // Assert
            Assert.Single(result);
            Assert.Contains("must be ≥ 1.0", result[0]);
            Assert.Contains("Alpha", result[0]);
        }

        [Fact]
        public void Validate_SpectralSubtractorWithAlphaEqualToOne_ReturnsEmptyList()
        {
            // Arrange
            var subtractor = new SpectralSubtractor(1024, 256)
            {
                Alpha = 1.0,
                SpectralFloor = 0.02
            };

            // Act
            var result = subtractor.Validate();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void Validate_SpectralSubtractorWithAlphaGreaterThanOne_ReturnsEmptyList()
        {
            // Arrange
            var subtractor = new SpectralSubtractor(1024, 256)
            {
                Alpha = 5.0,
                SpectralFloor = 0.02
            };

            // Act
            var result = subtractor.Validate();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void Validate_SpectralSubtractorWithSpectralFloorLessThanZero_ReturnsError()
        {
            // Arrange
            var subtractor = new SpectralSubtractor(1024, 256)
            {
                Alpha = 2.0,
                SpectralFloor = -0.1
            };

            // Act
            var result = subtractor.Validate();

            // Assert
            Assert.Single(result);
            Assert.Contains("must be in range [0, 1]", result[0]);
            Assert.Contains("SpectralFloor", result[0]);
        }

        [Fact]
        public void Validate_SpectralSubtractorWithSpectralFloorEqualToZero_ReturnsEmptyList()
        {
            // Arrange
            var subtractor = new SpectralSubtractor(1024, 256)
            {
                Alpha = 2.0,
                SpectralFloor = 0.0
            };

            // Act
            var result = subtractor.Validate();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void Validate_SpectralSubtractorWithSpectralFloorEqualToOne_ReturnsEmptyList()
        {
            // Arrange
            var subtractor = new SpectralSubtractor(1024, 256)
            {
                Alpha = 2.0,
                SpectralFloor = 1.0
            };

            // Act
            var result = subtractor.Validate();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void Validate_SpectralSubtractorWithSpectralFloorGreaterThanOne_ReturnsError()
        {
            // Arrange
            var subtractor = new SpectralSubtractor(1024, 256)
            {
                Alpha = 2.0,
                SpectralFloor = 1.1
            };

            // Act
            var result = subtractor.Validate();

            // Assert
            Assert.Single(result);
            Assert.Contains("must be in range [0, 1]", result[0]);
            Assert.Contains("SpectralFloor", result[0]);
        }

        [Fact]
        public void Validate_NullSpectralSubtractor_ThrowsArgumentNullException()
        {
            // Arrange
            SpectralSubtractor? subtractor = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => subtractor.Validate());
        }

        [Fact]
        public void IsValid_ValidSpectralSubtractor_ReturnsTrue()
        {
            // Arrange
            var subtractor = new SpectralSubtractor(1024, 256)
            {
                Alpha = 2.0,
                SpectralFloor = 0.02
            };

            // Act
            var result = subtractor.IsValid();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValid_InvalidAlpha_ReturnsFalse()
        {
            // Arrange
            var subtractor = new SpectralSubtractor(1024, 256)
            {
                Alpha = 0.5,
                SpectralFloor = 0.02
            };

            // Act
            var result = subtractor.IsValid();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValid_InvalidSpectralFloor_ReturnsFalse()
        {
            // Arrange
            var subtractor = new SpectralSubtractor(1024, 256)
            {
                Alpha = 2.0,
                SpectralFloor = 1.1
            };

            // Act
            var result = subtractor.IsValid();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValid_NullSpectralSubtractor_ReturnsFalse()
        {
            // Arrange
            SpectralSubtractor? subtractor = null;

            // Act
            var result = subtractor.IsValid();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void EnsureValid_ValidSpectralSubtractor_DoesNotThrow()
        {
            // Arrange
            var subtractor = new SpectralSubtractor(1024, 256)
            {
                Alpha = 2.0,
                SpectralFloor = 0.02
            };

            // Act & Assert
            subtractor.EnsureValid(); // Should not throw
        }

        [Fact]
        public void EnsureValid_InvalidAlpha_ThrowsArgumentException()
        {
            // Arrange
            var subtractor = new SpectralSubtractor(1024, 256)
            {
                Alpha = 0.5,
                SpectralFloor = 0.02
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => subtractor.EnsureValid());
            Assert.Contains("SpectralSubtractor is invalid", exception.Message);
            Assert.Contains("Alpha", exception.Message);
        }

        [Fact]
        public void EnsureValid_InvalidSpectralFloor_ThrowsArgumentException()
        {
            // Arrange
            var subtractor = new SpectralSubtractor(1024, 256)
            {
                Alpha = 2.0,
                SpectralFloor = 1.1
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => subtractor.EnsureValid());
            Assert.Contains("SpectralSubtractor is invalid", exception.Message);
            Assert.Contains("SpectralFloor", exception.Message);
        }

        [Fact]
        public void EnsureValid_NullSpectralSubtractor_ThrowsArgumentNullException()
        {
            // Arrange
            SpectralSubtractor? subtractor = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => subtractor.EnsureValid());
        }

        [Fact]
        public void ValidateNoiseProfile_ValidNoiseProfile_ReturnsEmptyList()
        {
            // Arrange
            var noiseProfile = new double[] { 0.1, 0.2, 0.3, 0.4 };

            // Act
            var result = noiseProfile.ValidateNoiseProfile();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void ValidateNoiseProfile_EmptyArray_ReturnsError()
        {
            // Arrange
            var noiseProfile = Array.Empty<double>();

            // Act
            var result = noiseProfile.ValidateNoiseProfile();

            // Assert
            Assert.Single(result);
            Assert.Contains("must not be empty", result[0]);
        }

        [Fact]
        public void ValidateNoiseProfile_NullNoiseProfile_ThrowsArgumentNullException()
        {
            // Arrange
            double[]? noiseProfile = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => noiseProfile.ValidateNoiseProfile());
        }

        [Fact]
        public void ValidateNoiseProfile_WithNegativeValue_ReturnsError()
        {
            // Arrange
            var noiseProfile = new double[] { 0.1, -0.2, 0.3, 0.4 };

            // Act
            var result = noiseProfile.ValidateNoiseProfile();

            // Assert
            Assert.Single(result);
            Assert.Contains("must not be negative", result[0]);
        }

        [Fact]
        public void ValidateNoiseProfile_WithNaN_ReturnsError()
        {
            // Arrange
            var noiseProfile = new double[] { 0.1, double.NaN, 0.3, 0.4 };

            // Act
            var result = noiseProfile.ValidateNoiseProfile();

            // Assert
            Assert.Single(result);
            Assert.Contains("must not be NaN", result[0]);
        }

        [Fact]
        public void ValidateNoiseProfile_WithPositiveInfinity_ReturnsError()
        {
            // Arrange
            var noiseProfile = new double[] { 0.1, double.PositiveInfinity, 0.3, 0.4 };

            // Act
            var result = noiseProfile.ValidateNoiseProfile();

            // Assert
            Assert.Single(result);
            Assert.Contains("must not be infinite", result[0]);
        }

        [Fact]
        public void ValidateNoiseProfile_WithNegativeInfinity_ReturnsError()
        {
            // Arrange
            var noiseProfile = new double[] { 0.1, double.NegativeInfinity, 0.3, 0.4 };

            // Act
            var result = noiseProfile.ValidateNoiseProfile();

            // Assert
            Assert.Single(result);
            Assert.Contains("must not be infinite", result[0]);
        }

        [Fact]
        public void ValidateNoiseProfile_WithCustomParamName_ReturnsErrorWithCorrectParamName()
        {
            // Arrange
            var noiseProfile = new double[] { 0.1, -0.2, 0.3, 0.4 };

            // Act
            var result = noiseProfile.ValidateNoiseProfile("customNoise");

            // Assert
            Assert.Single(result);
            Assert.Contains("customNoise", result[0]);
            Assert.Contains("must not be negative", result[0]);
        }

        [Fact]
        public void IsValidNoiseProfile_ValidNoiseProfile_ReturnsTrue()
        {
            // Arrange
            var noiseProfile = new double[] { 0.1, 0.2, 0.3, 0.4 };

            // Act
            var result = noiseProfile.IsValidNoiseProfile();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidNoiseProfile_EmptyArray_ReturnsFalse()
        {
            // Arrange
            var noiseProfile = Array.Empty<double>();

            // Act
            var result = noiseProfile.IsValidNoiseProfile();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidNoiseProfile_NullNoiseProfile_ReturnsFalse()
        {
            // Arrange
            double[]? noiseProfile = null;

            // Act
            var result = noiseProfile.IsValidNoiseProfile();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidNoiseProfile_WithInvalidValues_ReturnsFalse()
        {
            // Arrange
            var noiseProfile = new double[] { 0.1, -0.2, double.NaN };

            // Act
            var result = noiseProfile.IsValidNoiseProfile();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void EnsureValidNoiseProfile_ValidNoiseProfile_DoesNotThrow()
        {
            // Arrange
            var noiseProfile = new double[] { 0.1, 0.2, 0.3, 0.4 };

            // Act & Assert
            noiseProfile.EnsureValidNoiseProfile(); // Should not throw
        }

        [Fact]
        public void EnsureValidNoiseProfile_EmptyArray_ThrowsArgumentException()
        {
            // Arrange
            var noiseProfile = Array.Empty<double>();

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => noiseProfile.EnsureValidNoiseProfile());
            Assert.Contains("Noise profile is invalid", exception.Message);
            Assert.Contains("must not be empty", exception.Message);
        }

        [Fact]
        public void EnsureValidNoiseProfile_NullNoiseProfile_ThrowsArgumentNullException()
        {
            // Arrange
            double[]? noiseProfile = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => noiseProfile.EnsureValidNoiseProfile());
        }

        [Fact]
        public void EnsureValidNoiseProfile_WithNegativeValue_ThrowsArgumentException()
        {
            // Arrange
            var noiseProfile = new double[] { 0.1, -0.2, 0.3, 0.4 };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => noiseProfile.EnsureValidNoiseProfile());
            Assert.Contains("Noise profile is invalid", exception.Message);
            Assert.Contains("must not be negative", exception.Message);
        }

        [Fact]
        public void EnsureValidNoiseProfile_WithCustomParamName_ThrowsArgumentExceptionWithCorrectParamName()
        {
            // Arrange
            var noiseProfile = new double[] { 0.1, -0.2, 0.3, 0.4 };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => noiseProfile.EnsureValidNoiseProfile("testProfile"));
            Assert.Contains("testProfile", exception.Message);
            Assert.Contains("must not be negative", exception.Message);
        }
    }
}
