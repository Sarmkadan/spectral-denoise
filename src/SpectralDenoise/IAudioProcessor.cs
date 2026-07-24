using System;

namespace SpectralDenoise;

/// <summary>
/// Interface for audio processing components that can process audio samples.
/// </summary>
public interface IAudioProcessor
{
    /// <summary>
    /// Process an audio signal with the given sample rate.
    /// </summary>
    /// <param name="samples">Input audio signal samples</param>
    /// <param name="sampleRate">Audio sample rate in Hz</param>
    /// <returns>Processed audio signal</returns>
    /// <exception cref="ArgumentNullException">Thrown when samples is null.</exception>
    /// <exception cref="ArgumentException">Thrown when samples is empty or sampleRate is not positive.</exception>
    float[] Process(float[] samples, int sampleRate);

    /// <summary>
    /// Reset the processor state (useful between files/chunks).
    /// </summary>
    void Reset();
}

/// <summary>
/// Interface for spectral processors that work with frequency-domain processing.
/// </summary>
public interface ISpectralProcessor : IAudioProcessor
{
    /// <summary>
    /// Gets the frame size (number of samples per analysis frame).
    /// </summary>
    int FrameSize { get; }

    /// <summary>
    /// Gets the hop size (number of samples between analysis frames).
    /// </summary>
    int Hop { get; }

    /// <summary>
    /// Resets the smoothing state of the processor.
    /// </summary>
    void ResetSmoothing();
}