using System;
using System.Linq;
using System.Numerics;

namespace SpectralDenoise;

/// <summary>
/// Spectral subtractor for noise reduction in audio signals.
/// </summary>
public sealed class SpectralSubtractor
{
    private readonly int _frameSize;
    private readonly int _hop;
    private readonly double[] _window;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpectralSubtractor"/> class.
    /// </summary>
    /// <param name="frameSize">The frame size in samples.</param>
    /// <param name="hop">The hop size (frame advance) in samples.</param>
    /// <param name="window">The analysis window function.</param>
    public SpectralSubtractor(int frameSize, int hop, double[] window)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (frameSize <= 0)
            throw new ArgumentException("Frame size must be a positive integer.", nameof(frameSize));

        if (hop <= 0)
            throw new ArgumentException("Hop size must be a positive integer.", nameof(hop));

        if (window.Length != frameSize)
            throw new ArgumentException("Window length must match frame size.", nameof(window));

        _frameSize = frameSize;
        _hop = hop;
        _window = window;
    }

    /// <summary>
    /// Gets the frame size used by this subtractor instance.
    /// </summary>
    public int FrameSize => _frameSize;

    /// <summary>
    /// Gets the hop size (frame advance) used by this subtractor instance.
    /// </summary>
    public int Hop => _hop;

    /// <summary>
    /// Gets the analysis window used by this subtractor instance.
    /// </summary>
    public double[] Window => _window;

    /// <summary>
    /// Resets the smoothing coefficients.
    /// </summary>
    public void ResetSmoothing()
    {
        // No-op
    }

    /// <summary>
    /// Estimates the noise profile from a noise-only sample.
    /// </summary>
    /// <param name="noiseOnly">The noise-only sample.</param>
    /// <returns>The estimated noise profile.</returns>
    public double[] EstimateNoiseProfile(ReadOnlySpan<float> noiseOnly)
    {
        ArgumentNullException.ThrowIfNull(noiseOnly);

        if (noiseOnly.IsEmpty)
            throw new ArgumentException("Noise sample cannot be empty.", nameof(noiseOnly));

        // ... (rest of the method remains the same)
