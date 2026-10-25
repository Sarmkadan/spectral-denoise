namespace SpectralDenoise;

/// <summary>
/// Construction-time configuration for <see cref="SpectralSubtractor"/>: STFT framing,
/// over-subtraction and the analysis window. Pass to the <see cref="SpectralSubtractor(SpectralSubtractorOptions)"/> constructor.
/// </summary>
public sealed record SpectralSubtractorOptions
{
    /// <summary>
    /// FFT / analysis frame size in samples. Must be a power of two.
    /// Default = 1024.
    /// </summary>
    public int FftSize { get; init; } = SpectralSubtractorConstants.DefaultFftSize;

    /// <summary>
    /// Number of samples between consecutive analysis frames. Must be positive.
    /// Default = 256.
    /// </summary>
    public int HopSize { get; init; } = SpectralSubtractorConstants.DefaultHopSize;

    /// <summary>
    /// Over-subtraction factor applied to the noise profile during subtraction. Default = 1.0.
    /// </summary>
    public double OverSubtraction { get; init; } = SpectralSubtractorConstants.DefaultOverSubtraction;

    /// <summary>
    /// Number of noise frames to average when estimating a noise profile.
    /// Reserved: not read by <see cref="SpectralSubtractor"/> yet; the noise profile is supplied by the caller.
    /// Default = null.
    /// </summary>
    public int? NoiseFrames { get; init; }

    /// <summary>
    /// Analysis window of length <see cref="FftSize"/>. Null selects the periodic Hann window.
    /// A supplied window must satisfy the COLA condition for <see cref="HopSize"/>.
    /// The array is copied on construction, so later changes to it do not affect the processor.
    /// Default = null.
    /// </summary>
    public double[]? Window { get; init; }
}
