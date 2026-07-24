using System;
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
public sealed class SpectralSubtractor : ISpectralProcessor
{
    private readonly int _frameSize;
    private readonly int _hop;
    private readonly double[] _window;
    private readonly double[] _prevGain;
    private readonly double[] _normalization;
    private double[]? _noiseProfile;

    /// <summary>Over‑subtraction factor. 1.0 = plain Boll. Higher = more aggressive.</summary>
    public double Alpha { get; set; } = 2.0;

    /// <summary>Spectral floor. Keeps a fraction of the original magnitude to
    /// mask musical noise. Range 0..1.</summary>
    public double Beta { get; set; } = 0.02;

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
    public DenoiseMode Mode { get; set; } = DenoiseMode.SpectralSubtraction;

    /// <summary>
    /// Attack time in milliseconds for gain smoothing. Controls how quickly gain increases.
    /// Default = 0 (no smoothing).
    /// </summary>
    public double AttackMs { get; set; } = 0;

    /// <summary>
    /// Release time in milliseconds for gain smoothing. Controls how quickly gain decreases.
    /// Default = 0 (no smoothing).
    /// </summary>
    public double ReleaseMs { get; set; } = 0;

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
    public double[] Window => _window;

    /// <summary>
    /// Validates the current configuration for common problems.
    /// </summary>
    /// <returns>A list of human-readable problem descriptions; empty if valid.</returns>
    public IReadOnlyList<string> Validate()
    {
        var problems = new List<string>();

        // Validate Alpha (over-subtraction factor)
        // Should be >= 1.0 (1.0 = plain Boll, higher = more aggressive)
        if (Alpha < 1.0)
        {
            problems.Add(
                $"Alpha must be ≥ 1.0 (over-subtraction factor, got {Alpha:F4}).");
        }

        // Validate SpectralFloor (spectral floor)
        // Should be in range [0, 1] (fraction of original magnitude to mask musical noise)
        if (SpectralFloor is < 0.0 or > 1.0)
        {
            problems.Add(
                $"SpectralFloor must be in range [0, 1] (spectral floor, got {SpectralFloor:F4}).");
        }

        // Validate frame size is a power of two
        if (!IsPowerOfTwo(FrameSize))
        {
            problems.Add(
                $"FrameSize must be a power of two (got {FrameSize}, which is not).");
        }

        // Validate frame size is within reasonable bounds
        if (FrameSize < 128 || FrameSize > 8192)
        {
            problems.Add(
                $"FrameSize should be between 128 and 8192 samples (got {FrameSize}).");
        }

        return problems;
    }

    /// <summary>
    /// Checks whether the current configuration is valid.
    /// </summary>
    /// <returns>True if valid; otherwise false.</returns>
    public bool IsValid() => Validate().Count == 0;

    /// <summary>
    /// Ensures that the current configuration is valid, throwing an <see cref="ArgumentException"/>
    /// with a detailed message listing all problems if it is not.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the configuration is invalid, containing a list of problems.</exception>
    public void EnsureValid()
    {
        var problems = Validate();
        if (problems.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"SpectralSubtractor configuration is invalid:{Environment.NewLine} - {string.Join($"{Environment.NewLine} - ", problems)}");
    }

    /// <summary>
    /// Checks if a number is a power of two.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>True if the value is a power of two; otherwise false.</returns>
    private static bool IsPowerOfTwo(int value)
    {
        if (value <= 0)
            return false;

        return (value & (value - 1)) == 0;
    }

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
    /// Reset the processor state (useful between files/chunks).
    /// </summary>
    public void Reset()
    {
        Array.Clear(_prevGain, 0, _prevGain.Length);
        _noiseProfile = null;
    }

    /// <summary>
    /// State for streaming processing that maintains overlap buffers between frames.
    /// </summary>
    public sealed class StreamingState
    {
        internal int _frameSize;
        internal int _hop;
        internal double[] _window;
        internal double[] _overlapBuffer;
        internal Complex[] _fftBuffer;
        internal int _overlapSamples;

        /// <summary>
        /// Initializes a new streaming state.
        /// </summary>
        /// <param name="frameSize">Frame size</param>
        /// <param name="hop">Hop size</param>
        /// <param name="window">Analysis window</param>
        internal StreamingState(int frameSize, int hop, double[] window)
        {
            _frameSize = frameSize;
            _hop = hop;
            _window = window;
            _overlapBuffer = new double[_hop];
            _fftBuffer = new Complex[frameSize];
            _overlapSamples = 0;
        }

        /// <summary>
        /// Gets the number of samples currently in the overlap buffer.
        /// </summary>
        public int OverlapSamples => _overlapSamples;

        /// <summary>
        /// Gets the overlap buffer (read-only).
        /// </summary>
        public ReadOnlySpan<double> OverlapBuffer => _overlapBuffer.AsSpan(0, _overlapSamples);

        /// <summary>
        /// Resets the streaming state (clears overlap buffers).
        /// </summary>
        public void Reset()
        {
            Array.Clear(_overlapBuffer, 0, _overlapBuffer.Length);
            _overlapSamples = 0;
        }

        /// <summary>
        /// Gets the FFT buffer for processing.
        /// </summary>
        internal Complex[] FftBuffer => _fftBuffer;

        /// <summary>
        /// Gets the window function.
        /// </summary>
        internal double[] Window => _window;

        /// <summary>
        /// Gets the frame size.
        /// </summary>
        internal int FrameSize => _frameSize;

        /// <summary>
        /// Gets the hop size.
        /// </summary>
        internal int Hop => _hop;
    }

    /// <summary>
    /// Creates a new streaming state for processing audio incrementally.
    /// </summary>
    /// <returns>A new streaming state instance</returns>
    public StreamingState CreateStreamingState()
    {
        return new StreamingState(_frameSize, _hop, _window);
    }

    /// <summary>
    /// Process an audio signal with the given sample rate.
    /// </summary>
    /// <param name="samples">Input audio signal</param>
    /// <param name="sampleRate">Audio sample rate in Hz</param>
    /// <returns>Processed audio signal</returns>
    /// <exception cref="ArgumentNullException">Thrown when samples is null.</exception>
    /// <exception cref="ArgumentException">Thrown when samples is empty, sampleRate is not positive, or noise profile is not set.</exception>
    public float[] Process(float[] samples, int sampleRate)
    {
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(sampleRate, 0);
        ArgumentOutOfRangeException.ThrowIfEqual(samples.Length, 0);
        ArgumentNullException.ThrowIfNull(_noiseProfile, nameof(_noiseProfile));

        return Process(samples.AsSpan(), _noiseProfile, null);
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
        ArgumentNullException.ThrowIfNull(noiseProfile);
        int bins = _frameSize / 2 + 1;
        if (noiseProfile.Length != bins)
            throw new ArgumentException("Noise profile bin count does not match frame size.");

        // Store noise profile for the IAudioProcessor.Process method
        _noiseProfile = noiseProfile;

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

    /// <summary>
    /// Processes audio incrementally using a streaming state that maintains overlap buffers.
    /// This allows processing of arbitrarily long audio streams without loading everything into memory.
    /// </summary>
    /// <param name="input">Input audio samples (can be shorter than frameSize)</param>
    /// <param name="state">Streaming state from previous calls</param>
    /// <param name="output">Output buffer to write processed samples to</param>
    /// <param name="outputOffset">Starting offset in output buffer</param>
    /// <param name="noiseProfile">Noise profile to use for denoising</param>
    /// <param name="progress">Optional progress reporter</param>
    /// <returns>The number of samples written to the output buffer</returns>
    /// <exception cref="ArgumentNullException">Thrown when input, state, output, or noiseProfile is null</exception>
    /// <exception cref="ArgumentException">Thrown when noise profile length doesn't match frame size</exception>
    public int ProcessBlock(
        ReadOnlySpan<float> input,
        StreamingState state,
        Span<float> output,
        int outputOffset,
        double[] noiseProfile,
        IProgress<double>? progress = null)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));
        if (state == null)
            throw new ArgumentNullException(nameof(state));
        if (output == null)
            throw new ArgumentNullException(nameof(output));
        if (noiseProfile == null)
            throw new ArgumentNullException(nameof(noiseProfile));

        int bins = _frameSize / 2 + 1;
        if (noiseProfile.Length != bins)
            throw new ArgumentException("Noise profile bin count does not match frame size.");

        int outputSamplesWritten = 0;
        int inputIndex = 0;

        // Process input samples in chunks
        while (inputIndex < input.Length)
        {
            // Fill the overlap buffer with new input samples
            int samplesToCopy = Math.Min(state.Hop - state._overlapSamples, input.Length - inputIndex);
            if (samplesToCopy > 0)
            {
                input.Slice(inputIndex, samplesToCopy).CopyTo(output.Slice(outputOffset + state._overlapSamples));
                inputIndex += samplesToCopy;
                state._overlapSamples += samplesToCopy;
            }

            // When we have a full frame, process it
            if (state._overlapSamples >= state.Hop)
            {
                // Copy overlap buffer to FFT buffer with windowing
                for (int i = 0; i < state.FrameSize; i++)
                {
                    int overlapIndex = i % state.Hop;
                    state.FftBuffer[i] = new Complex(output[outputOffset + overlapIndex] * state.Window[i], 0.0);
                }

                // Perform FFT
                Fft.Forward(state.FftBuffer);

                // Apply denoising
                for (int b = 0; b < bins; b++)
                {
                    double mag = state.FftBuffer[b].Magnitude;
                    double phase = state.FftBuffer[b].Phase;

                    double cleaned;
                    double currentGain = 1.0;

                    if (Mode == DenoiseMode.Wiener)
                    {
                        // Wiener filter
                        double signalPower = mag * mag;
                        double noisePower = noiseProfile[b] * noiseProfile[b];
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

                    // Apply gain smoothing if enabled
                    if (AttackMs > 0 || ReleaseMs > 0)
                    {
                        double targetGain = currentGain;
                        double prev = _prevGain[b];
                        double coeff = (targetGain > prev) ? CalculateSmoothingCoefficient(AttackMs, 44100, isAttack: true) :
                                                       CalculateSmoothingCoefficient(ReleaseMs, 44100, isAttack: false);
                        double smoothedGain = prev + coeff * (targetGain - prev);
                        _prevGain[b] = smoothedGain;
                        cleaned *= smoothedGain;
                    }

                    state.FftBuffer[b] = Complex.FromPolarCoordinates(cleaned, phase);
                    if (b > 0 && b < bins - 1)
                        state.FftBuffer[state.FrameSize - b] = Complex.Conjugate(state.FftBuffer[b]);
                }

                // Inverse FFT
                Fft.Inverse(state.FftBuffer);

                // Accumulate output (overlap-add)
                for (int i = 0; i < state.FrameSize; i++)
                {
                    int outputIndex = outputOffset + (state._overlapSamples - state.Hop) + i;
                    if (outputIndex < output.Length)
                    {
                        output[outputIndex] += (float)(state.FftBuffer[i].Real * state.Window[i]);
                    }
                }

                // Update overlap buffer for next frame
                int samplesToKeep = state.FrameSize - state.Hop;
                if (samplesToKeep > 0)
                {
                    // Shift remaining samples to beginning of overlap buffer
                    for (int i = 0; i < samplesToKeep; i++)
                    {
                        state._overlapBuffer[i] = output[outputOffset + state.Hop + i];
                    }
                    state._overlapSamples = samplesToKeep;
                }
                else
                {
                    state._overlapSamples = 0;
                }

                outputSamplesWritten += state.Hop;
            }
            else
            {
                // Not enough samples yet, wait for more input
                break;
            }
        }

        if (progress != null && input.Length > 0)
        {
            progress.Report((double)inputIndex / input.Length);
        }

        return outputSamplesWritten;
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