namespace SpectralDenoise;

/// <summary>
/// Startup configuration for an audio pipeline: sample rate and STFT framing plus the
/// spectral subtraction parameters. Validate with <see cref="PipelineOptionsValidation.Validate"/>
/// or let <see cref="AudioPipeline"/> enforce it on construction.
/// </summary>
/// <param name="SampleRate">Audio sample rate in Hz. Must be greater than zero.</param>
/// <param name="FrameSize">Analysis frame size in samples. Must be a power of two.</param>
/// <param name="Hop">Number of samples between frames. Must be in (0, FrameSize].</param>
/// <param name="OverSubtractionFactor">Over-subtraction factor (alpha). Must be a finite number >= 1.</param>
/// <param name="SpectralFloor">Minimum fraction of the original magnitude kept (beta). Must be in [0, 1).</param>
public readonly record struct PipelineOptions(
    int SampleRate,
    int FrameSize,
    int Hop,
    double OverSubtractionFactor,
    double SpectralFloor)
{
    /// <summary>
    /// Gets the default options: 44.1 kHz, 1024-sample frames, hop 256, alpha 2.0, floor 0.02.
    /// </summary>
    public static PipelineOptions Default => new(
        PipelineOptionsConstants.DefaultSampleRate,
        PipelineOptionsConstants.DefaultFrameSize,
        PipelineOptionsConstants.DefaultHop,
        PipelineOptionsConstants.DefaultOverSubtractionFactor,
        PipelineOptionsConstants.DefaultSpectralFloor);
}
