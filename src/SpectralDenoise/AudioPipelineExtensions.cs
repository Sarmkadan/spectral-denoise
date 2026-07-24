using System;

namespace SpectralDenoise;

/// <summary>
/// Extension methods for creating and working with <see cref="AudioPipeline"/> instances.
/// </summary>
public static class AudioPipelineExtensions
{
    /// <summary>
    /// Creates an audio processing pipeline from a sequence of processors.
    /// </summary>
    /// <param name="processors">Array of audio processors to chain together.</param>
    /// <returns>A new <see cref="AudioPipeline"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when processors is null.</exception>
    /// <exception cref="ArgumentException">Thrown when processors array is empty or contains null processors.</exception>
    public static AudioPipeline CreatePipeline(this IAudioProcessor[] processors)
    {
        return new AudioPipeline(processors);
    }

    /// <summary>
    /// Creates an audio processing pipeline with a descriptive name.
    /// </summary>
    /// <param name="name">Descriptive name for the pipeline.</param>
    /// <param name="processors">Array of audio processors to chain together.</param>
    /// <returns>A new <see cref="AudioPipeline"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when name or processors is null.</exception>
    /// <exception cref="ArgumentException">Thrown when processors array is empty or contains null processors.</exception>
    public static AudioPipeline CreatePipeline(this string name, params IAudioProcessor[] processors)
    {
        return new AudioPipeline(name, processors);
    }

    /// <summary>
    /// Chains another processor to the end of this pipeline.
    /// </summary>
    /// <param name="pipeline">The existing pipeline.</param>
    /// <param name="processor">The processor to append.</param>
    /// <returns>A new <see cref="AudioPipeline"/> with the additional processor.</returns>
    /// <exception cref="ArgumentNullException">Thrown when pipeline or processor is null.</exception>
    public static AudioPipeline Then(this AudioPipeline pipeline, IAudioProcessor processor)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(processor);

        var processors = new IAudioProcessor[pipeline.Processors.Count + 1];
        pipeline.Processors.ToArray().CopyTo(processors, 0);
        processors[pipeline.Processors.Count] = processor;

        return new AudioPipeline(pipeline.Name, processors);
    }

    /// <summary>
    /// Creates a pipeline with a noise gate followed by spectral subtraction.
    /// This is a convenience method for the common use case of noise reduction.
    /// </summary>
    /// <param name="gate">The noise gate processor.</param>
    /// <param name="subtractor">The spectral subtractor processor.</param>
    /// <returns>A new <see cref="AudioPipeline"/> with gate-then-subtract processing.</returns>
    /// <exception cref="ArgumentNullException">Thrown when gate or subtractor is null.</exception>
    public static AudioPipeline CreateNoiseReductionPipeline(this NoiseGate gate, SpectralSubtractor subtractor)
    {
        ArgumentNullException.ThrowIfNull(gate);
        ArgumentNullException.ThrowIfNull(subtractor);

        return new AudioPipeline("Noise Reduction Pipeline", gate, subtractor);
    }

    /// <summary>
    /// Creates a pipeline with a noise gate followed by spectral subtraction.
    /// This is a convenience method for the common use case of noise reduction.
    /// </summary>
    /// <param name="gateThresholdDb">Noise gate threshold in dB.</param>
    /// <param name="gateAttackMs">Noise gate attack time in milliseconds.</param>
    /// <param name="gateReleaseMs">Noise gate release time in milliseconds.</param>
    /// <param name="subtractor">The spectral subtractor processor.</param>
    /// <returns>A new <see cref="AudioPipeline"/> with gate-then-subtract processing.</returns>
    /// <exception cref="ArgumentNullException">Thrown when subtractor is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when parameters are invalid.</exception>
    public static AudioPipeline CreateNoiseReductionPipeline(
        this int sampleRate,
        float gateThresholdDb = -45f,
        float gateAttackMs = 5f,
        float gateReleaseMs = 100f,
        SpectralSubtractor? subtractor = null)
    {
        var gate = new NoiseGate(sampleRate, gateThresholdDb, gateAttackMs, gateReleaseMs);
        subtractor ??= new SpectralSubtractor();

        return new AudioPipeline("Noise Reduction Pipeline", gate, subtractor);
    }
}