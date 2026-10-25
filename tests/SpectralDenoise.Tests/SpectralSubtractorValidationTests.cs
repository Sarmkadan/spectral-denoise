using System;
using SpectralDenoise;
using Xunit;
using static SpectralDenoise.Tests.SpectralSubtractorValidationTestsConstants;

namespace SpectralDenoise.Tests
{
    /// <summary>
    /// Test suite for validating the SpectralSubtractor class and noise profile validation.
    /// </summary>
    public class SpectralSubtractorValidationTests : ISpectralSubtractorValidationTests
    {
        /// <summary>
        /// Tests that validating a SpectralSubtractor with valid Alpha (≥1.0) and SpectralFloor ([0,1]) returns an empty validation error list.
        /// </summary>
        [Fact]
        public void Validate_SpectralSubtractorWithValidProperties_ReturnsEmptyList()
        {
            // Arrange
            var subtractor = new SpectralSubtractor(FrameSize, HopSize)
            {
                Alpha = ValidAlpha,
                SpectralFloor = ValidSpectralFloor
            };

            // Act
            var result = subtractor.Validate();

            // Assert
            Assert.Empty(result);
        }

        /// <summary>
        /// Tests that validating a SpectralSubtractor with Alpha < 1.0 returns a validation error list containing an error about Alpha.
        /// </summary>
        [Fact]
        public void Validate_SpectralSubtractorWithAlphaLessThanOne_ReturnsError()
        {
            // Arrange
            var subtractor = new SpectralSubtractor(FrameSize, HopSize)
            {
                Alpha = AlphaBelowOne,
                SpectralFloor = ValidSpectralFloor
            };

            // Act
            var result = subtractor.Validate();

            // Assert
            Assert.Single(result);
            Assert.Contains(AlphaErrorMessageFragment, result[0]);
            Assert.Contains(AlphaPropertyName, result[0]);
        }

        /// <summary>
        /// Tests that validating a SpectralSubtractor with Alpha = 1.0 returns an empty validation error list.
        /// </summary>
        [Fact]
        public void Validate_SpectralSubtractorWithAlphaEqualToOne_ReturnsEmptyList()
        {
            // Arrange
            var subtractor = new SpectralSubtractor(FrameSize, HopSize)
            {
                Alpha = AlphaEqualToOne,
                SpectralFloor = ValidSpectralFloor
            };

            // Act
            var result = subtractor.Validate();

            // Assert
            Assert.Empty(result);
        }

        /// <summary>
        /// Tests that validating a SpectralSubtractor with Alpha > 1.0 returns an empty validation error list.
        /// </summary>
        [Fact]
        public void Validate_SpectralSubtractorWithAlphaGreaterThanOne_ReturnsEmptyList()
        {
            // Arrange
            var subtractor = new SpectralSubtractor(FrameSize, HopSize)
            {
                Alpha = AlphaAboveOne,
                SpectralFloor = ValidSpectralFloor
            };

            // Act
            var result = subtractor.Validate();

            // Assert
            Assert.Empty(result);
        }

        /// <summary>
        /// Tests that validating a SpectralSubtractor with SpectralFloor = 0 returns an empty validation error list.
        /// </summary>
        [Fact]
        public void Validate_SpectralSubtractorWithSpectralFloorEqualToZero_ReturnsEmptyList()
        {
            // Arrange
            var subtractor = new SpectralSubtractor(FrameSize, HopSize)
            {
                Alpha = ValidAlpha,
                SpectralFloor = SpectralFloorZero
            };

            // Act
            var result = subtractor.Validate();

            // Assert
            Assert.Empty(result);
        }

        /// <summary>
        /// Tests that validating a SpectralSubtractor with SpectralFloor = 1 returns an empty validation error list.
        /// </summary>
        [Fact]
        public void Validate_SpectralSubtractorWithSpectralFloorEqualToOne_ReturnsEmptyList()
        {
            // Arrange
            var subtractor = new SpectralSubtractor(FrameSize, HopSize)
            {
                Alpha = ValidAlpha,
                SpectralFloor = SpectralFloorOne
            };

            // Act
            var result = subtractor.Validate();

            // Assert
            Assert.Empty(result);
        }

        /// <summary>
        /// Tests that validating a null SpectralSubtractor throws an ArgumentNullException.
        /// </summary>
        [Fact]
        public void Validate_NullSpectralSubtractor_ThrowsArgumentNullException()
        {
            // Arrange
            SpectralSubtractor? subtractor = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => subtractor.Validate());
        }

        /// <summary>
        /// Tests that IsValid returns true for a valid SpectralSubtractor.
        /// </summary>
        [Fact]
        public void IsValid_ValidSpectralSubtractor_ReturnsTrue()
        {
            // Arrange
            var subtractor = new SpectralSubtractor(FrameSize, HopSize)
            {
                Alpha = ValidAlpha,
                SpectralFloor = ValidSpectralFloor
            };

            // Act
            var result = subtractor.IsValid();

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// Tests that IsValid returns false for a SpectralSubtractor with invalid Alpha (<1.0).
        /// </summary>
        [Fact]
        public void IsValid_InvalidAlpha_ReturnsFalse()
        {
            // Arrange
            var subtractor = new SpectralSubtractor(FrameSize, HopSize)
            {
                Alpha = AlphaBelowOne,
                SpectralFloor = ValidSpectralFloor
            };

            // Act
            var result = subtractor.IsValid();

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// Tests that IsValid returns false for a null SpectralSubtractor.
        /// </summary>
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

        /// <summary>
        /// Tests that EnsureValid does not throw for a valid SpectralSubtractor.
        /// </summary>
        [Fact]
        public void EnsureValid_ValidSpectralSubtractor_DoesNotThrow()
        {
            // Arrange
            var subtractor = new SpectralSubtractor(FrameSize, HopSize)
            {
                Alpha = ValidAlpha,
                SpectralFloor = ValidSpectralFloor
            };

            // Act & Assert
            subtractor.EnsureValid(); // Should not throw
        }

        /// <summary>
        /// Tests that EnsureValid throws an ArgumentException for a SpectralSubtractor with invalid Alpha (<1.0).
        /// </summary>
        [Fact]
        public void EnsureValid_InvalidAlpha_ThrowsArgumentException()
        {
            // Arrange
            var subtractor = new SpectralSubtractor(FrameSize, HopSize)
            {
                Alpha = AlphaBelowOne,
                SpectralFloor = ValidSpectralFloor
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => subtractor.EnsureValid());
            Assert.Contains(SubtractorInvalidMessage, exception.Message);
            Assert.Contains(AlphaPropertyName, exception.Message);
        }

        /// <summary>
        /// Tests that EnsureValid throws an ArgumentNullException for a null SpectralSubtractor.
        /// </summary>
        [Fact]
        public void EnsureValid_NullSpectralSubtractor_ThrowsArgumentNullException()
        {
            // Arrange
            SpectralSubtractor? subtractor = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => subtractor.EnsureValid());
        }

        /// <summary>
        /// Tests that validating a valid noise profile returns an empty validation error list.
        /// </summary>
        [Fact]
        public void ValidateNoiseProfile_ValidNoiseProfile_ReturnsEmptyList()
        {
            // Arrange
            var noiseProfile = ValidNoiseProfile;

            // Act
            var result = noiseProfile.ValidateNoiseProfile();

            // Assert
            Assert.Empty(result);
        }

        /// <summary>
        /// Tests that validating an empty noise profile array returns a validation error list containing an error about the array being empty.
        /// </summary>
        [Fact]
        public void ValidateNoiseProfile_EmptyArray_ReturnsError()
        {
            // Arrange
            var noiseProfile = Array.Empty<double>();

            // Act
            var result = noiseProfile.ValidateNoiseProfile();

            // Assert
            Assert.Single(result);
            Assert.Contains(MustNotBeEmptyMessage, result[0]);
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
            var noiseProfile = NegativeValueNoiseProfile;

            // Act
            var result = noiseProfile.ValidateNoiseProfile();

            // Assert
            Assert.Single(result);
            Assert.Contains(MustNotBeNegativeMessage, result[0]);
        }

        [Fact]
        public void ValidateNoiseProfile_WithNaN_ReturnsError()
        {
            // Arrange
            var noiseProfile = NaNValueNoiseProfile;

            // Act
            var result = noiseProfile.ValidateNoiseProfile();

            // Assert
            Assert.Single(result);
            Assert.Contains(MustNotBeNaNMessage, result[0]);
        }

        [Fact]
        public void ValidateNoiseProfile_WithPositiveInfinity_ReturnsError()
        {
            // Arrange
            var noiseProfile = PositiveInfinityNoiseProfile;

            // Act
            var result = noiseProfile.ValidateNoiseProfile();

            // Assert
            Assert.Single(result);
            Assert.Contains(MustNotBeInfiniteMessage, result[0]);
        }

        [Fact]
        public void ValidateNoiseProfile_WithNegativeInfinity_ReturnsError()
        {
            // Arrange
            var noiseProfile = NegativeInfinityNoiseProfile;

            // Act
            var result = noiseProfile.ValidateNoiseProfile();

            // Assert
            Assert.Single(result);
            Assert.Contains(MustNotBeInfiniteMessage, result[0]);
        }

        [Fact]
        public void ValidateNoiseProfile_WithCustomParamName_ReturnsErrorWithCorrectParamName()
        {
            // Arrange
            var noiseProfile = NegativeValueNoiseProfile;

            // Act
            var result = noiseProfile.ValidateNoiseProfile(CustomNoiseParamName);

            // Assert
            Assert.Single(result);
            Assert.Contains(CustomNoiseParamName, result[0]);
            Assert.Contains(MustNotBeNegativeMessage, result[0]);
        }

        /// <summary>
        /// Tests that IsValidNoiseProfile returns true for a valid noise profile.
        /// </summary>
        [Fact]
        public void IsValidNoiseProfile_ValidNoiseProfile_ReturnsTrue()
        {
            // Arrange
            var noiseProfile = ValidNoiseProfile;

            // Act
            var result = noiseProfile.IsValidNoiseProfile();

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// Tests that IsValidNoiseProfile returns false for an empty noise profile array.
        /// </summary>
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

        /// <summary>
        /// Tests that IsValidNoiseProfile returns false for a null noise profile.
        /// </summary>
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

        /// <summary>
        /// Tests that IsValidNoiseProfile returns false for a noise profile containing invalid values (negative or NaN).
        /// </summary>
        [Fact]
        public void IsValidNoiseProfile_WithInvalidValues_ReturnsFalse()
        {
            // Arrange
            var noiseProfile = InvalidValuesNoiseProfile;

            // Act
            var result = noiseProfile.IsValidNoiseProfile();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void EnsureValidNoiseProfile_ValidNoiseProfile_DoesNotThrow()
        {
            // Arrange
            var noiseProfile = ValidNoiseProfile;

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
            Assert.Contains(NoiseProfileInvalidMessage, exception.Message);
            Assert.Contains(MustNotBeEmptyMessage, exception.Message);
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
            var noiseProfile = NegativeValueNoiseProfile;

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => noiseProfile.EnsureValidNoiseProfile());
            Assert.Contains(NoiseProfileInvalidMessage, exception.Message);
            Assert.Contains(MustNotBeNegativeMessage, exception.Message);
        }

        [Fact]
        public void EnsureValidNoiseProfile_WithCustomParamName_ThrowsArgumentExceptionWithCorrectParamName()
        {
            // Arrange
            var noiseProfile = NegativeValueNoiseProfile;

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => noiseProfile.EnsureValidNoiseProfile(TestProfileParamName));
            Assert.Contains(TestProfileParamName, exception.Message);
            Assert.Contains(MustNotBeNegativeMessage, exception.Message);
        }
    }
}
