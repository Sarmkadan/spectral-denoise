namespace SpectralDenoise.Tests
{
    public interface ISpectralSubtractorJsonExtensionsTests
    {
        void ToJson_HappyPath_ReturnsJsonString();
        void ToJson_NullInput_ThrowsArgumentNullException();
        void FromJson_HappyPath_ReturnsSpectralSubtractor();
        void FromJson_NullInput_ReturnsNull();
        void FromJson_EmptyJson_ReturnsNull();
        void TryFromJson_HappyPath_ReturnsTrue();
        void TryFromJson_NullInput_ReturnsFalse();
        void TryFromJson_EmptyJson_ReturnsFalse();
    }
}
