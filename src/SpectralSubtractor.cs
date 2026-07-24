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
    private readonly double _alpha;
    private readonly double _beta;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpectralSubtractor"/> class.
    /// </summary>
    /// <param name="frameSize">The frame size in samples.</param>
    /// <param name="hop">The hop size (frame advance) in samples.</param>
    /// <param name="window">The analysis window function.</param>
    /// <param name="alpha">The over-subtraction factor (default is 1.0).</param>
    /// <param name="beta">The spectral floor factor (default is 0.01).</param>
    public SpectralSubtractor(int frameSize, int hop, double[] window, double alpha = 1.0, double beta = 0.01)
    {
        ArgumentNullException.ThrowIfNull(window);

        if (frameSize <= 0)
            throw new ArgumentException("Frame size must be a positive integer.", nameof(frameSize));

        if (hop <= 0)
            throw new ArgumentException("Hop size must be a positive integer.", nameof(hop));

        if (window.Length != frameSize)
            throw new ArgumentException("Window length must match frame size.", nameof(window));

        if (alpha < 0)
            throw new ArgumentException("Over-subtraction factor must be non-negative.", nameof(alpha));

        if (beta < 0)
            throw new ArgumentException("Spectral floor factor must be non-negative.", nameof(beta));

        _frameSize = frameSize;
        _hop = hop;
        _window = window;
        _alpha = alpha;
        _beta = beta;
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
    /// Gets or sets the over-subtraction factor.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if the value is negative.</exception>
    public double Alpha
    {
        get => _alpha;
        set
        {
            if (value < 0)
                throw new ArgumentException("Over-subtraction factor must be non-negative.", nameof(value));
            _alpha = value;
        }
    }

    /// <summary>
    /// Gets or sets the spectral floor factor.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if the value is negative.</exception>
    public double Beta
    {
        get => _beta;
        set
        {
            if (value < 0)
                throw new ArgumentException("Spectral floor factor must be non-negative.", nameof(value));
            _beta = value;
        }
    }

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
    }

    /// <summary>
    /// Processes the input signal using the spectral subtraction algorithm.
    /// </summary>
    /// <param name="signal">The input signal.</param>
    /// <param name="noiseProfile">The estimated noise profile.</param>
    /// <param name="progress">The progress reporter.</param>
    /// <returns>The processed signal.</returns>
    public float[] Process(ReadOnlySpan<float> signal, double[] noiseProfile, IProgress<double>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(signal);
        ArgumentNullException.ThrowIfNull(noiseProfile);

        if (signal.IsEmpty)
            throw new ArgumentException("Signal cannot be empty.", nameof(signal));

        if (noiseProfile.Length != _frameSize / 2 + 1)
            throw new ArgumentException("Noise profile length must match frame size.", nameof(noiseProfile));

        var result = new float[signal.Length];

        for (int i = 0; i < signal.Length; i += _hop)
        {
            var frame = signal.Slice(i, Math.Min(_frameSize, signal.Length - i));
            var fft = new Complex[_frameSize];
            for (int j = 0; j < frame.Length; j++)
            {
                fft[j] = new Complex(frame[j] * _window[j], 0);
            }
            Fft.Forward(fft);

            for (int j = 0; j < fft.Length / 2 + 1; j++)
            {
                var magnitude = fft[j].Magnitude;
                var noiseMagnitude = noiseProfile[j];
                var subtractedMagnitude = Math.Max(magnitude - _alpha * noiseMagnitude, _beta * noiseMagnitude);
                fft[j] = new Complex(subtractedMagnitude, fft[j].Phase);
            }

            Fft.Inverse(fft);
            for (int j = 0; j < frame.Length; j++)
            {
                result[i + j] = (float)(fft[j].Real / _window[j]);
            }
        }

        return result;
    }
}
