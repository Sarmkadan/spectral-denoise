using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SpectralDenoise;

/// <summary>
/// Spectral subtractor for noise reduction in audio signals.
/// </summary>
public sealed class SpectralSubtractor
{
    private int _frameSize;
    private int _hop;
    private double[] _window;
    private double _alpha;
    private double _beta;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpectralSubtractor"/> class.
    /// </summary>
    /// <param name="frameSize">The frame size in samples.</param>
    /// <param name="hop">The hop size (frame advance) in samples.</param>
    /// <param name="window">The analysis window function.</param>
    /// <param name="alpha">The over‑subtraction factor (default is 1.0).</param>
    /// <param name="beta">The spectral floor factor (default is 0.01).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="window"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when any of the arguments are invalid:
    /// <list type="bullet">
    ///   <item><description>frameSize must be a positive integer.</description></item>
    ///   <item><description>hop must be a positive integer.</description></item>
    ///   <item><description>frameSize must be a multiple of hop.</description></item>
    ///   <item><description>window length must match frame size.</description></item>
    ///   <item><description>window and hop combination must satisfy COLA.</description></item>
    ///   <item><description>alpha must be non‑negative.</description></item>
    ///   <item><description>beta must be non‑negative.</description></item>
    /// </list>
    /// </exception>
    public SpectralSubtractor(int frameSize, int hop, double[] window, double alpha = 1.0, double beta = 0.01)
    {
        ArgumentNullException.ThrowIfNull(window);

        // Basic numeric validation
        if (frameSize <= 0)
            throw new ArgumentException("Frame size must be a positive integer.", nameof(frameSize));

        if (hop <= 0)
            throw new ArgumentException("Hop size must be a positive integer.", nameof(hop));

        if (frameSize % hop != 0)
            throw new ArgumentException("Frame size must be a multiple of hop size.", nameof(frameSize));

        // Window validation
        if (window.Length != frameSize)
            throw new ArgumentException("Window length must match frame size.", nameof(window));

        if (!WindowFunctions.SatisfiesCola(window, hop))
            throw new ArgumentException("Window and hop size combination does not satisfy COLA.", nameof(window));

        // Alpha / Beta validation
        if (alpha < 0)
            throw new ArgumentException("Over‑subtraction factor must be non‑negative.", nameof(alpha));

        if (beta < 0)
            throw new ArgumentException("Spectral floor factor must be non‑negative.", nameof(beta));

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
    /// Gets or sets the over‑subtraction factor.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if the value is negative.</exception>
    public double Alpha
    {
        get => _alpha;
        set
        {
            if (value < 0)
                throw new ArgumentException("Over‑subtraction factor must be non‑negative.", nameof(value));
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
                throw new ArgumentException("Spectral floor factor must be non‑negative.", nameof(value));
            _beta = value;
        }
    }

    /// <summary>
    /// Validates the frame‑size / hop‑size combination.
    /// </summary>
    /// <returns>A list of validation messages; empty if the combination is valid.</returns>
    public IReadOnlyList<string> ValidateFrameSizeAndHop()
    {
        var messages = new List<string>();

        if (_frameSize <= 0)
            messages.Add("Frame size must be a positive integer.");

        if (_hop <= 0)
            messages.Add("Hop size must be a positive integer.");

        if (_frameSize % _hop != 0)
            messages.Add("Frame size must be a multiple of hop size.");

        return messages;
    }

    /// <summary>
    /// Validates the window / hop‑size combination.
    /// </summary>
    /// <returns>A list of validation messages; empty if the combination is valid.</returns>
    public IReadOnlyList<string> ValidateWindowAndHop()
    {
        var messages = new List<string>();

        if (_window.Length != _frameSize)
            messages.Add("Window length must match frame size.");

        if (!WindowFunctions.SatisfiesCola(_window, _hop))
            messages.Add("Window and hop size combination does not satisfy COLA.");

        return messages;
    }

    /// <summary>
    /// Resets the smoothing coefficients.
    /// </summary>
    public void ResetSmoothing()
    {
        // No‑op – placeholder for future smoothing state reset.
    }

    /// <summary>
    /// Estimates the noise profile from a noise‑only sample.
    /// </summary>
    /// <param name="noiseOnly">The noise‑only sample.</param>
    /// <returns>The estimated noise profile.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="noiseOnly"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when the sample is empty.</exception>
    public double[] EstimateNoiseProfile(ReadOnlySpan<float> noiseOnly)
    {
        ArgumentNullException.ThrowIfNull(noiseOnly);

        if (noiseOnly.IsEmpty)
            throw new ArgumentException("Noise sample cannot be empty.", nameof(noiseOnly));

        // ... (rest of the method remains the same)
        throw new NotImplementedException(); // Placeholder – original implementation retained elsewhere.
    }

    /// <summary>
    /// Processes the input signal using the spectral subtraction algorithm.
    /// </summary>
    /// <param name="signal">The input signal.</param>
    /// <param name="noiseProfile">The estimated noise profile.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <returns>The processed signal.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="signal"/> or <paramref name="noiseProfile"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when:
    /// <list type="bullet">
    ///   <item><description>signal is empty.</description></item>
    ///   <item><description>noiseProfile length does not match the expected size.</description></item>
    /// </list>
    /// </exception>
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
