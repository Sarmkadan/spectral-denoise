namespace SpectralDenoise.Tests
{
    public interface ISpectralSubtractorTests
    {
        void Constructor_NonPowerOfTwo_ThrowsArgumentException();
        void DefaultPropertyValues_AreAsExpected();
        void ResetSmoothing_DoesNotThrow();
        void EstimateNoiseProfile_InsufficientLength_ThrowsInvalidOperationException();
        void EstimateNoiseProfile_WhiteNoise_ReturnsPositiveValues();
        void EstimateNoiseProfile_Silence_ReturnsAllZeros();
        void Process_MismatchedNoiseProfile_ThrowsArgumentException();
        void Process_EmptySignal_ReturnsEmptyArray();
        void Process_OverSubtractionFactorZero_NoAttenuation();
        void Process_WienerMode_ReducesNoise();
    }
}
