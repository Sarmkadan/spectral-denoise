namespace SpectralDenoise.Tests;

/// <summary>
/// Shared constants for <see cref="SpectralSubtractorValidationTests"/>: STFT sizes, valid and invalid
/// Alpha/SpectralFloor values, noise profile samples, and expected validation message fragments.
/// </summary>
internal static class SpectralSubtractorValidationTestsConstants
{
    // STFT configuration used to construct every SpectralSubtractor under test
    public const int FrameSize = 1024;
    public const int HopSize = 256;

    // Valid SpectralSubtractor parameters
    public const double ValidAlpha = 2.0;
    public const double ValidSpectralFloor = 0.02;

    // Alpha boundary values
    public const double AlphaBelowOne = 0.5;
    public const double AlphaEqualToOne = 1.0;
    public const double AlphaAboveOne = 5.0;

    // SpectralFloor boundary values
    public const double SpectralFloorZero = 0.0;
    public const double SpectralFloorOne = 1.0;

    // Expected exception and validation message fragments
    public const string AlphaPropertyName = "Alpha";
    public const string AlphaErrorMessageFragment = "must be ≥ 1.0";
    public const string SubtractorInvalidMessage = "SpectralSubtractor is invalid";
    public const string NoiseProfileInvalidMessage = "Noise profile is invalid";
    public const string MustNotBeEmptyMessage = "must not be empty";
    public const string MustNotBeNegativeMessage = "must not be negative";
    public const string MustNotBeNaNMessage = "must not be NaN";
    public const string MustNotBeInfiniteMessage = "must not be infinite";

    // Custom parameter names passed to noise profile validation
    public const string CustomNoiseParamName = "customNoise";
    public const string TestProfileParamName = "testProfile";

    // Noise profile samples
    public static readonly double[] ValidNoiseProfile = { 0.1, 0.2, 0.3, 0.4 };
    public static readonly double[] NegativeValueNoiseProfile = { 0.1, -0.2, 0.3, 0.4 };
    public static readonly double[] NaNValueNoiseProfile = { 0.1, double.NaN, 0.3, 0.4 };
    public static readonly double[] PositiveInfinityNoiseProfile = { 0.1, double.PositiveInfinity, 0.3, 0.4 };
    public static readonly double[] NegativeInfinityNoiseProfile = { 0.1, double.NegativeInfinity, 0.3, 0.4 };
    public static readonly double[] InvalidValuesNoiseProfile = { 0.1, -0.2, double.NaN };
}
