namespace SpectralDenoise.Tests
{
    public interface IFftTests
    {
        void Fft_RealSineAtExactBin_GivesPeakAtThatBin();
        void Fft_ZeroSignal_ReturnsAllZeros();
        void Fft_ForwardInverse_ReturnsOriginalSignal();
    }
}
