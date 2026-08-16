namespace SpectralDenoise.Tests
{
    public interface IWindowFunctionsTests
    {
        public void HannWindow_EndpointsNearZero();
        public void HannWindow_Periodic();
        public void HannWindow_PeakAtCenter();
        public void HannWindow_AllValuesBetweenZeroAndOne();
    }
}
