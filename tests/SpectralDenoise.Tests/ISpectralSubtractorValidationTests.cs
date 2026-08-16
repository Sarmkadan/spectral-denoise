namespace SpectralDenoise.Tests
{
    public interface ISpectralSubtractorValidationTests
    {
        void Validate_SpectralSubtractorWithValidProperties_ReturnsEmptyList();
        void Validate_SpectralSubtractorWithAlphaLessThanOne_ReturnsError();
        void Validate_SpectralSubtractorWithAlphaEqualToOne_ReturnsEmptyList();
        void Validate_SpectralSubtractorWithAlphaGreaterThanOne_ReturnsEmptyList();
        void Validate_SpectralSubtractorWithSpectralFloorLessThanZero_ReturnsError();
        void Validate_SpectralSubtractorWithSpectralFloorEqualToZero_ReturnsEmptyList();
        void Validate_SpectralSubtractorWithSpectralFloorEqualToOne_ReturnsEmptyList();
        void Validate_SpectralSubtractorWithSpectralFloorGreaterThanOne_ReturnsError();
        void Validate_NullSpectralSubtractor_ThrowsArgumentNullException();
        void IsValid_ValidSpectralSubtractor_ReturnsTrue();
        void IsValid_InvalidAlpha_ReturnsFalse();
        void IsValid_InvalidSpectralFloor_ReturnsFalse();
        void IsValid_NullSpectralSubtractor_ReturnsFalse();
        void EnsureValid_ValidSpectralSubtractor_DoesNotThrow();
        void EnsureValid_InvalidAlpha_ThrowsArgumentException();
        void EnsureValid_InvalidSpectralFloor_ThrowsArgumentException();
        void EnsureValid_NullSpectralSubtractor_ThrowsArgumentNullException();
        void ValidateNoiseProfile_ValidNoiseProfile_ReturnsEmptyList();
        void ValidateNoiseProfile_EmptyArray_ReturnsError();
        void ValidateNoiseProfile_NullNoiseProfile_ThrowsArgumentNullException();
    }
}
