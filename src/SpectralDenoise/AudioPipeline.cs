using System;
using System.Diagnostics;

namespace SpectralDenoise;

/// <summary>
/// Represents a chain of audio processors that can be applied sequentially to audio data.
/// </summary>
public sealed class AudioPipeline : IAudioProcessor
{
    private readonly IAudioProcessor[] _processors;
    private readonly string? _name;

    /// <summary>
    /// Initializes a new audio processing pipeline.
    /// </summary>
    /// <param name="processors">Array of audio processors to chain together. Processors are applied in order.</param>
    /// <exception cref="ArgumentNullException">Thrown when processors array is null.</exception>
    /// <exception cref="ArgumentException">Thrown when processors array is empty or contains null processors.</exception>
    public AudioPipeline(params IAudioProcessor[] processors)
        : this(null, processors)
    {
    }

    /// <summary>
    /// Initializes a new audio processing pipeline with a descriptive name.
    /// </summary>
    /// <param name="name">Optional descriptive name for the pipeline (useful for debugging).</param>
    /// <param name="processors">Array of audio processors to chain together. Processors are applied in order.</param>
    /// <exception cref="ArgumentNullException">Thrown when processors array is null.</exception>
    /// <exception cref="ArgumentException">Thrown when processors array is empty or contains null processors.</exception>
    public AudioPipeline(string? name, params IAudioProcessor[] processors)
    {
        ArgumentNullException.ThrowIfNull(processors);

        if (processors.Length == 0)
            throw new ArgumentException("Pipeline must contain at least one processor.", nameof(processors));

        foreach (var processor in processors)
        {
            ArgumentNullException.ThrowIfNull(processor);
        }

        _processors = processors;
        _name = name;
    }

    /// <summary>
    /// Gets the array of processors in this pipeline.
    /// </summary>
    public IReadOnlyList<IAudioProcessor> Processors => _processors;

    /// <summary>
    /// Gets the optional name of this pipeline.
    /// </summary>
    public string? Name => _name;

    /// <summary>
    /// Gets or sets a callback invoked after each processor completes, with the pipeline name or
    /// processor type name, the zero-based processor index, and the elapsed processing time.
    /// </summary>
    public Action<string, int, TimeSpan>? OnProcessorCompleted { get; set; }

    /// <summary>
    /// Process audio samples through the entire pipeline sequentially.
    /// </summary>
    /// <param name="samples">Input audio signal samples.</param>
    /// <param name="sampleRate">Audio sample rate in Hz.</param>
    /// <returns>Processed audio signal after passing through all pipeline stages.</returns>
    /// <exception cref="ArgumentNullException">Thrown when samples is null.</exception>
    /// <exception cref="ArgumentException">Thrown when samples is empty or sampleRate is not positive.</exception>
    public float[] Process(float[] samples, int sampleRate)
    {
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(sampleRate, 0);
        ArgumentOutOfRangeException.ThrowIfEqual(samples.Length, 0);

        float[] current = samples;

        for (int index = 0; index < _processors.Length; index++)
        {
            IAudioProcessor processor = _processors[index];
            Stopwatch stopwatch = Stopwatch.StartNew();
            current = processor.Process(current, sampleRate);
            stopwatch.Stop();
            OnProcessorCompleted?.Invoke(_name ?? processor.GetType().Name, index, stopwatch.Elapsed);
        }

        return current;
    }

    /// <summary>
    /// Reset all processors in the pipeline.
    /// </summary>
    public void Reset()
    {
        foreach (var processor in _processors)
        {
            processor.Reset();
        }
    }

    /// <summary>
    /// Returns a string representation of this pipeline.
    /// </summary>
    /// <returns>String describing the pipeline and its processors.</returns>
    public override string ToString()
    {
        if (_name != null)
        {
            return $"AudioPipeline '{_name}' with {_processors.Length} processor(s)";
        }

        return $"AudioPipeline with {_processors.Length} processor(s)";
    }
}
