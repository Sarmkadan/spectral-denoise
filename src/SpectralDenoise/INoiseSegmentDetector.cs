using System;

namespace SpectralDenoise;

/// <summary>
/// Locates the part of a signal that is most likely noise-only, for use as a noise estimate.
/// </summary>
public interface INoiseSegmentDetector
{
    /// <summary>
    /// Finds the noise-only segment of a mono signal.
    /// </summary>
    /// <param name="signal">Mono samples to search.</param>
    /// <param name="sampleRate">Audio sample rate in Hz.</param>
    /// <param name="seconds">Desired length of the noise segment in seconds.</param>
    /// <returns>The segment to pass to <see cref="SpectralSubtractor.EstimateNoiseProfile"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when sampleRate is not positive or seconds is not a finite positive number.</exception>
    /// <exception cref="ArgumentException">Thrown when signal is empty.</exception>
    NoiseSegment Detect(ReadOnlySpan<float> signal, int sampleRate, double seconds);
}
