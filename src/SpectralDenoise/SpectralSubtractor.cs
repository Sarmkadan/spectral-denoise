using System.Numerics;

namespace SpectralDenoise;

/// <summary>
/// Denoising mode for spectral processing
/// </summary>
public enum DenoiseMode
{
    /// <summary>
    /// Classic spectral subtraction (Boll, 1979)
    /// </summary>
    SpectralSubtraction,

    /// <summary>
    /// Wiener filtering: gain = SNR/(SNR+1) per frequency bin
    /// </summary>
    Wiener
}

/// <summary>
/// Classic magnitude spectral subtraction (Boll, 1979) on an STFT.
///
/// Idea: estimate the noise magnitude spectrum from a "quiet" region of the
/// recording, then for every analysis frame subtract that estimate from the
/// frame magnitude while keeping the original phase.
///
/// This is deliberately the textbook version. It works but it hisses and
/// leaves "musical noise" all over the place - see README.
/// </summary>
public sealed class SpectralSubtractor
{
    private readonly int _frameSize;
    private readonly int _hop;
    private readonly double[] _window;
    private readonly double[] _prevGain;
    private readonly double[] _normalization;

    /// <summary>Over‑subtraction factor. 1.0 = plain Boll. Higher = more aggressive.</summary>
    public double Alpha { get; init; } = 2.0;

    /// <summary>Spectral floor. Keeps a fraction of the original magnitude to
    /// mask musical noise. Range 0..1.</summary>
    public double Beta { get; init; } = 0.02;

    /// <summary>
    /// Over‑subtraction factor. Multiplies the noise profile during subtraction.
    /// Default = 1.0.
    /// </summary>
    public double OverSubtractionFactor { get; set; } = 1.0;

    /// <summary>
    /// Spectral floor. Minimum fraction of the original magnitude kept,
    /// preventing musical‑noise zeros. Default = 0.02.
/// Range: 0..1.
    /// </summary>
    public double SpectralFloor { get; set; } = 0.02;

    /// <summary>
    /// Denoising mode: SpectralSubtraction (classic) or Wiener (Wiener filter).
    /// Default = SpectralSubtraction (maintains backward compatibility).
    /// </summary>
    public DenoiseMode Mode { get; init; } = DenoiseMode.SpectralSubtraction;

    /// <summary>
    /// Attack time in milliseconds for gain smoothing. Controls how quickly gain increases.
    /// Default = 0 (no smoothing).
    /// </summary>
    public double AttackMs { get; init; } = 0;

    /// <summary>
    /// Release time in milliseconds for gain smoothing. Controls how quickly gain decreases.
    /// Default = 0 (no smoothing).
    /// </summary>
    public double ReleaseMs { get; init; } = 0;

    /// <summary>
    /// Gets the frame size (number of samples per analysis frame).
    /// </summary>
    public int FrameSize => _frameSize;

    /// <summary>
    /// Gets the hop size (number of samples between analysis frames).
    /// </summary>
    public int Hop
    {
        get => _hop;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0);
            if (_frameSize != 0) // Only validate if frameSize has been set (in constructor)
            {
                if (!WindowFunctions.SatisfiesCola(_window, value))
                {
                    throw new ArgumentException(
                        $"Hop size {value} does not satisfy the Constant Overlap-Add (COLA) condition " +
                        $"with the current window. The sum of squared window values should be approximately " +
                        $"equal to the hop size for perfect reconstruction. " +
                        $"Use a periodic window (e.g., WindowFunctions.HannPeriodic(frameSize)) and ensure hop size is compatible. " +
                        $"Common COLA-compatible combinations: hop = frameSize/4 with periodic Hann, hop = frameSize/2 with periodic Hann.");
                }
            }
            _hop = value;
        }
    }

    /// <summary>
    /// Gets the analysis window function.
    /// </summary>
    public ReadOnlySpan<double> Window => _window;

    /// <summary>
    /// Validates that the window/overlap combination satisfies the Constant Overlap-Add (COLA) condition.
    /// If not, throws an exception with a detailed message about the issue and how to fix it.
    /// </summary>
    /// <param name="window">The window function</param>
    /// <param name="hop">Hop size</param>
    /// <exception cref="ArgumentException">Thrown when COLA condition is not satisfied</exception>
    private static void ValidateCola(ReadOnlySpan<double> window, int hop)
    {
        if (!WindowFunctions.SatisfiesCola(window, hop))
        {
            double sum = 0.0;
            for (int i = 0; i < window.Length; i++)
            {
                sum += window[i] * window[i];
            }

            throw new ArgumentException(
                $"Window/overlap combination does not satisfy the Constant Overlap-Add (COLA) condition. " +
                $"The sum of squared window values is {sum:F6}, but should be approximately {hop} for perfect reconstruction. " +
                $"This causes amplitude modulation artifacts in the output. " +
                $"Use a periodic window (e.g., WindowFunctions.HannPeriodic(frameSize)) and ensure hop size is compatible. " +
                $"Common COLA-compatible combinations: hop = frameSize/4 with periodic Hann, hop = frameSize/2 with periodic Hann.");
        }
    }

    /// <summary>
    /// Calculates the one-pole smoothing coefficient from time constant in milliseconds.
    /// </summary>
    /// <param name="timeMs">Time constant in milliseconds</param>
    /// <param name="sampleRate">Audio sample rate in Hz</param>
    /// <param name="isAttack">True for attack (rise time), false for release (fall time)</param>
    /// <returns>Smoothing coefficient (0 to 1)</returns>
    private static double CalculateSmoothingCoefficient(double timeMs, double sampleRate, bool isAttack)
    {
        if (timeMs <= 0)
            return 1.0; // No smoothing - immediate response

        // Convert milliseconds to seconds
        double timeSeconds = timeMs / 1000.0;
        // Calculate coefficient: coeff = 1 - exp(-1/(tau * fs))
        // where tau = timeSeconds, fs = sampleRate
        double tau = timeSeconds;
        double coeff = 1.0 - Math.Exp(-1.0 / (tau * sampleRate));

        return coeff;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpectralSubtractor"/> class.
    /// </summary>
    /// <param name="frameSize">Frame size (must be a power of two).</param>
    /// <param name="hop">Hop size (number of samples between frames).</param>
    /// <exception cref="ArgumentException">Thrown when frameSize is not a power of two or when window/overlap combination violates COLA.</exception>
    public SpectralSubtractor(int frameSize = 1024, int hop = 256)
    {
        if ((frameSize & (frameSize - 1)) != 0)
            throw new ArgumentException("frameSize must be a power of two.", nameof(frameSize));

        if (hop <= 0)
            throw new ArgumentOutOfRangeException(nameof(hop), "Hop must be positive.");

        _frameSize = frameSize;
        _window = WindowFunctions.HannPeriodic(frameSize);
        _prevGain = new double[frameSize / 2 + 1];

        // Validate COLA condition
        ValidateCola(_window, hop);

        _hop = hop;

        // Pre-compute normalization array for perfect reconstruction
        _normalization = WindowFunctions.ComputeWindowSumSquared(_window, _hop, 1024 * 10);
    }

    /// <summary>
    /// Resets the smoothing state (previous gain values). Useful when processing
    /// a new audio segment or when the signal characteristics change significantly.
    /// </summary>
    public void ResetSmoothing()
    {
        Array.Clear(_prevGain, 0, _prevGain.Length);
    }

    /// <summary>
    /// Estimate a noise magnitude profile from a mono sample span, assumed to
    /// be noise-only (e.g. leading silence).
    /// </summary>
    public double[] EstimateNoiseProfile(ReadOnlySpan<float> noiseOnly)
    {
        int bins = _frameSize / 2 + 1;
        var profile = new double[bins];
        int frames = 0;

        for (int start = 0; start + _frameSize <= noiseOnly.Length; start += _hop)
        {
            var spec = Analyze(noiseOnly.Slice(start, _frameSize));
            for (int b = 0; b < bins; b++)
                profile[b] += spec[b].Magnitude;
            frames++;
        }

        if (frames == 0)
            throw new InvalidOperationException(
                "Noise region shorter than one frame - give me more leading silence.");

        for (int b = 0; b < bins; b++)
            profile[b] /= frames;

        return profile;
    }

    /// <summary>
    /// Denoise a whole mono signal via overlap-add. Returns a new buffer the
    /// same length as the input.
    /// </summary>
    /// <param name="signal">Input signal</param>
    /// <param name="noiseProfile">Noise magnitude profile</param>
    /// <param name="progress">Optional progress reporter (fraction of frames processed)</param>
    /// <exception cref="ArgumentException">Thrown when noise profile length doesn't match frame size.</exception>
    public float[] Process(ReadOnlySpan<float> signal, double[] noiseProfile, IProgress<double>? progress = null)
    {
        int bins = _frameSize / 2 + 1;
        if (noiseProfile.Length != bins)
            throw new ArgumentException("Noise profile bin count does not match frame size.");

        var output = new float[signal.Length];

        double sampleRate = 44100; // Standard sample rate for time constant calculations
        double attackCoeff = CalculateSmoothingCoefficient(AttackMs, sampleRate, isAttack: true);
        double releaseCoeff = CalculateSmoothingCoefficient(ReleaseMs, sampleRate, isAttack: false);

        // Determine total number of frames for progress reporting
        int totalFrames = 0;
        if (signal.Length >= _frameSize)
            totalFrames = ((signal.Length - _frameSize) / _hop) + 1;

        int processedFrames = 0;

        for (int start = 0; start + _frameSize <= signal.Length; start += _hop)
        {
            var spec = Analyze(signal.Slice(start, _frameSize));

            // Apply denoising based on mode
            for (int b = 0; b < bins; b++)
            {
                double mag = spec[b].Magnitude;
                double phase = spec[b].Phase;

                double cleaned;
                double currentGain = 1.0;

                if (Mode == DenoiseMode.Wiener)
                {
                    // Wiener filter: gain = SNR / (SNR + 1)
                    // where SNR = signal_power / noise_power
                    double signalPower = mag * mag;
                    double noisePower = noiseProfile[b] * noiseProfile[b];

                    // Avoid division by zero and negative SNR
                    double snr = signalPower > 1e-20 ? signalPower / noisePower : 0.0;
                    currentGain = snr / (snr + 1.0);

                    cleaned = mag * currentGain;
                }
                else
                {
                    // Classic spectral subtraction
                    double rawGain = Math.Max(0, mag - OverSubtractionFactor * noiseProfile[b]) / mag;
                    currentGain = Math.Max(0, rawGain);
                    cleaned = mag - OverSubtractionFactor * noiseProfile[b];
                }

                // Apply spectral floor
                double floor = SpectralFloor * mag;
                if (cleaned < floor) cleaned = floor;

                // Apply one-pole smoothing to gain
                if (AttackMs > 0 || ReleaseMs > 0)
                {
                    // Apply smoothing based on whether gain is increasing or decreasing
                    double targetGain = currentGain;
                    double prev = _prevGain[b];
                    double coeff = (targetGain > prev) ? attackCoeff : releaseCoeff;
                    double smoothedGain = prev + coeff * (targetGain - prev);
                    _prevGain[b] = smoothedGain;

                    // Apply smoothed gain to magnitude
                    spec[b] = Complex.FromPolarCoordinates(cleaned * smoothedGain, phase);
                }
                else
                {
                    spec[b] = Complex.FromPolarCoordinates(cleaned, phase);
                }

                if (b > 0 && b < bins - 1)
                    spec[_frameSize - b] = Complex.Conjugate(spec[b]);
            }

            Fft.Inverse(spec);

            for (int i = 0; i < _frameSize; i++)
            {
                output[start + i] += (float)(spec[i].Real * _window[i]);
            }

            // Report progress
            processedFrames++;
            if (progress != null && totalFrames > 0)
            {
                progress.Report((double)processedFrames / totalFrames);
            }
        }

        // undo the analysis+synthesis window weighting using pre-computed normalization
        // This ensures perfect reconstruction when COLA is satisfied
        for (int i = 0; i < output.Length; i++)
        {
            // Use the pre-computed normalization value for this position
            // If we're beyond the pre-computed array, compute it on the fly
            double norm = i < _normalization.Length ? _normalization[i] : WindowFunctions.ComputeWindowSumSquared(_window, _hop, 1)[i];
            if (norm > 1e-6)
                output[i] /= (float)norm;
        }

        return output;
    }

    private Complex[] Analyze(ReadOnlySpan<float> frame)
    {
        var buffer = new Complex[_frameSize];
        for (int i = 0; i < _frameSize; i++)
            buffer[i] = new Complex(frame[i] * _window[i], 0.0);
        Fft.Forward(buffer);
        return buffer;
    }
}
