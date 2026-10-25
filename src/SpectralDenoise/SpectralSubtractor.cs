using System;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace SpectralDenoise;

/// <summary>
/// Denoising mode for spectral processing. The algorithm consists of four phases:
/// 1. Framing: Divide audio into overlapping frames for analysis.
/// 2. Noise estimate: Calculate noise profile from quiet regions.
/// 3. Subtraction: Reduce noise by modifying spectral magnitudes.
/// 4. Overlap-add: Reconstruct audio from processed frames.
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
/// <remarks>
/// This class is not thread-safe. Do not share an instance across multiple threads
/// without external synchronization.
/// </remarks>
public sealed class SpectralSubtractor : ISpectralProcessor, ISpectralSubtractor
{
    private readonly int _frameSize;
    private readonly int _hop;
    private readonly double[] _window;
    private readonly double[] _prevGain;
    private readonly double _normalizationConstant; // Sum of squared window values (COLA normalization)
    private readonly Complex[] _analysisBuffer;
    private double[]? _noiseProfile;
    private double _spectralFloor = SpectralSubtractorConstants.DefaultSpectralFloor;

    /// <summary>
    /// Over-subtraction factor used by the classic Boll algorithm.
    /// Default = 2.0. Must be >= 1.0.
    /// </summary>
    [JsonPropertyOrder(0)]
    public double Alpha { get; set; } = 2.0;

    /// <summary>
    /// Spectral floor fraction (legacy name).
    /// Default = 0.02. Range: 0..1.
    /// </summary>
    [JsonPropertyOrder(1)]
    public double Beta { get; set; } = 0.02;

    /// <summary>
    /// Over‑subtraction factor. Multiplies the noise profile during subtraction.
    /// Default = 1.0.
    /// </summary>
    [JsonPropertyOrder(3)]
    public double OverSubtractionFactor { get; set; } = SpectralSubtractorConstants.DefaultOverSubtraction;

    /// <summary>
    /// Spectral floor. Minimum fraction of the original magnitude kept,
    /// preventing musical‑noise zeros. Default = 0.02.
    /// Range: [0, 1] inclusive (the minimum gain applied to each bin).
    /// Validated on assignment: NaN, infinities and values outside [0, 1] throw
    /// <see cref="ArgumentOutOfRangeException"/>, so <see cref="Process(ReadOnlySpan{float}, double[], IProgress{double}?)"/> never re-checks it.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is outside [0, 1] or not a number.</exception>
    [JsonPropertyOrder(4)]
    public double SpectralFloor
    {
        get => _spectralFloor;
        set
        {
            if (!SpectralSubtractorValidation.IsValidSpectralFloor(value))
                throw new ArgumentOutOfRangeException(nameof(SpectralFloor), value, SpectralSubtractorConstants.SpectralFloorRangeMessage);

            _spectralFloor = value;
        }
    }

    /// <summary>
    /// Denoising mode: SpectralSubtraction (classic) or Wiener (Wiener filter).
    /// Default = SpectralSubtraction (maintains backward compatibility).
    /// </summary>
    [JsonPropertyOrder(2)]
    public DenoiseMode Mode { get; set; } = DenoiseMode.SpectralSubtraction;

    /// <summary>
    /// Attack time in milliseconds for gain smoothing. Controls how quickly gain increases.
    /// Default = 0 (no smoothing).
    /// </summary>
    [JsonPropertyOrder(5)]
    public double AttackMs { get; set; } = 0;

    /// <summary>
    /// Release time in milliseconds for gain smoothing. Controls how quickly gain decreases.
    /// Default = 0 (no smoothing).
    /// </summary>
    [JsonPropertyOrder(6)]
    public double ReleaseMs { get; set; } = 0;

    /// <summary>
    /// Gets the frame size (number of samples per analysis frame).
    /// </summary>
    [JsonIgnore]
    public int FrameSize => _frameSize;

    /// <summary>
    /// Gets the hop size (number of samples between analysis frames).
    /// </summary>
    [JsonIgnore]
    public int Hop
    {
        get => _hop;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0);
            _hop = value;
        }
    }

    /// <summary>
    /// Gets the analysis window function.
    /// </summary>
    [JsonIgnore]
    public double[] Window => _window;


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
                string.Format(SpectralSubtractorConstants.ValidateColaErrorMessage, sum, hop));
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
    /// Initializes a new instance of the <see cref="SpectralSubtractor"/> class from the given options.
    /// </summary>
    /// <param name="options">Framing, over-subtraction and window configuration.</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    /// <exception cref="ArgumentException">Thrown when FftSize is not a power of two, when a supplied window has the wrong length, or when the window/overlap combination violates COLA.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when HopSize is not positive.</exception>
    public SpectralSubtractor(SpectralSubtractorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        int frameSize = options.FftSize;
        int hop = options.HopSize;

        if ((frameSize & (frameSize - 1)) != 0)
            throw new ArgumentException(SpectralSubtractorConstants.FrameSizePowerOfTwoMessage, nameof(options.FftSize));

        if (hop <= 0)
            throw new ArgumentOutOfRangeException(nameof(options.HopSize), SpectralSubtractorConstants.HopPositiveMessage);

        if (options.Window is null)
        {
            _window = WindowFunctions.HannPeriodic(frameSize);
        }
        else
        {
            if (options.Window.Length != frameSize)
                throw new ArgumentException($"Window length {options.Window.Length} does not match FftSize {frameSize}.", nameof(options.Window));

            ValidateCola(options.Window, hop);
            _window = (double[])options.Window.Clone();
        }

        _frameSize = frameSize;
        _prevGain = new double[frameSize / 2 + 1];
        _analysisBuffer = new Complex[frameSize];
        OverSubtractionFactor = options.OverSubtraction;

        _hop = hop;

        // Pre-compute normalization constant for perfect reconstruction.
        // This is the per-sample overlap sum of squared window values.
        // For a COLA-compatible window, this is constant across all interior positions.
        // Compute it by summing w[n + k*hop]^2 for a representative interior position.
        double overlapSum = 0.0;
        int testPos = _frameSize; // An interior position that is covered by full overlaps
        for (int start = 0; start + _frameSize <= testPos + _frameSize; start += _hop)
        {
            int idx = testPos - start;
            if (idx >= 0 && idx < _frameSize)
                overlapSum += _window[idx] * _window[idx];
        }
        _normalizationConstant = overlapSum;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpectralSubtractor"/> class with the default periodic Hann window.
    /// Convenience overload equivalent to passing <see cref="SpectralSubtractorOptions"/> with FftSize = <paramref name="frameSize"/> and HopSize = <paramref name="hop"/>.
    /// </summary>
    /// <param name="frameSize">Frame size (must be a power of two).</param>
    /// <param name="hop">Hop size (number of samples between frames).</param>
    /// <exception cref="ArgumentException">Thrown when frameSize is not a power of two or when window/overlap combination violates COLA.</exception>
    public SpectralSubtractor(int frameSize = SpectralSubtractorConstants.DefaultFftSize, int hop = SpectralSubtractorConstants.DefaultHopSize)
        : this(new SpectralSubtractorOptions { FftSize = frameSize, HopSize = hop })
    {
    }

    /// <summary>
    /// Called after JSON deserialization to validate the untrusted input.
    /// </summary>
    [JsonConstructor]
    private SpectralSubtractor() : this(new SpectralSubtractorOptions())
    {
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
        /// Gets the overlap buffer (read‑only).
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
    /// be noise‑only (e.g. leading silence).
    /// </summary>
    public double[] EstimateNoiseProfile(ReadOnlySpan<float> noiseOnly)
    {
        int bins = _frameSize / 2 + 1;
        var profile = new double[bins];
        int frames = 0;

        for (int start = 0; start + _frameSize <= noiseOnly.Length; start += _hop)
        {
            AnalyzeInto(noiseOnly.Slice(start, _frameSize));
            var spec = _analysisBuffer;
            for (int b = 0; b < bins; b++)
                profile[b] += spec[b].Magnitude;
            frames++;
        }

        if (frames == 0)
            throw new InvalidOperationException(SpectralSubtractorConstants.NoiseRegionTooShortMessage);

        for (int b = 0; b < bins; b++)
            profile[b] /= frames;

        return profile;
    }

    /// <summary>
    /// Denoise a whole mono signal via overlap‑add. Returns a new buffer the
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

        // Pad signal to ensure full overlap at boundaries for perfect reconstruction
        int pad = _frameSize - _hop;
        var padded = new float[pad + signal.Length + pad];
        signal.CopyTo(padded.AsSpan(pad));

        var paddedOutput = new float[padded.Length];

        double sampleRate = SpectralSubtractorConstants.DefaultSampleRate; // Standard sample rate for time constant calculations
        double attackCoeff = CalculateSmoothingCoefficient(AttackMs, sampleRate, isAttack: true);
        double releaseCoeff = CalculateSmoothingCoefficient(ReleaseMs, sampleRate, isAttack: false);

        // Determine total number of frames for progress reporting
        int totalFrames = 0;
        if (padded.Length >= _frameSize)
            totalFrames = ((padded.Length - _frameSize) / _hop) + 1;

        int processedFrames = 0;

        for (int start = 0; start + _frameSize <= padded.Length; start += _hop)
        {
            AnalyzeInto(padded.AsSpan(start, _frameSize));
            var spec = _analysisBuffer;

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
                    double snr = signalPower > SpectralSubtractorConstants.PowerEpsilon ? signalPower / noisePower : 0.0;
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
                    double gain = (mag > 0) ? (cleaned * smoothedGain / mag) : 0.0;
                    spec[b] = spec[b] * gain;
                }
                else
                {
                    // Apply gain directly to preserve complex value precision
                    double gain = (mag > 0) ? (cleaned / mag) : 0.0;
                    spec[b] = spec[b] * gain;
                }

                if (b > 0 && b < bins - 1)
                    spec[_frameSize - b] = Complex.Conjugate(spec[b]);
            }

            Fft.Inverse(spec);

            for (int i = 0; i < _frameSize; i++)
            {
                paddedOutput[start + i] += (float)(spec[i].Real * _window[i]);
            }

            // Report progress
            processedFrames++;
            if (progress != null && totalFrames > 0)
            {
                progress.Report((double)processedFrames / totalFrames);
            }
        }

        // Undo the analysis+synthesis window weighting using per-sample normalization.
        var windowNorm = new double[padded.Length];
        for (int start = 0; start + _frameSize <= padded.Length; start += _hop)
        {
            for (int i = 0; i < _frameSize; i++)
                windowNorm[start + i] += _window[i] * _window[i];
        }
        for (int i = 0; i < padded.Length; i++)
        {
            if (windowNorm[i] > SpectralSubtractorConstants.NormalizationEpsilon)
                paddedOutput[i] /= (float)windowNorm[i];
        }

        // Extract the original signal region (remove padding)
        var output = new float[signal.Length];
        Array.Copy(paddedOutput, pad, output, 0, signal.Length);
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
    /// <exception cref="ArgumentNullException">Thrown when state or noiseProfile is null</exception>
    /// <exception cref="ArgumentException">Thrown when noise profile length doesn't match frame size</exception>
    public int ProcessBlock(
        ReadOnlySpan<float> input,
        StreamingState state,
        Span<float> output,
        int outputOffset,
        double[] noiseProfile,
        IProgress<double>? progress = null)
    {
        if (state == null)
            throw new ArgumentNullException(nameof(state));
        if (noiseProfile == null)
            throw new ArgumentNullException(nameof(noiseProfile));

        int bins = _frameSize / 2 + 1;
        if (noiseProfile.Length != bins)
            throw new ArgumentException("Noise profile bin count does not match frame size.");

        int outputSamplesWritten = 0;
        int inputIndex = 0;

        // Pre‑compute smoothing coefficients once per block processing
        double attackCoeff = CalculateSmoothingCoefficient(AttackMs, SpectralSubtractorConstants.DefaultSampleRate, isAttack: true);
        double releaseCoeff = CalculateSmoothingCoefficient(ReleaseMs, SpectralSubtractorConstants.DefaultSampleRate, isAttack: false);

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
                        double snr = signalPower > SpectralSubtractorConstants.PowerEpsilon ? signalPower / noisePower : 0.0;
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
                        double coeff = (targetGain > prev) ? attackCoeff : releaseCoeff;
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

                // Accumulate output (overlap‑add)
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

        // Apply normalization to the portion of the output that was written in this call
        if (SpectralSubtractorConstants.NormalizationEpsilon > 0 && outputSamplesWritten > 0)
        {
            for (int i = outputOffset; i < outputOffset + outputSamplesWritten; i++)
            {
                output[i] = (float)(output[i] / SpectralSubtractorConstants.NormalizationEpsilon);
            }
        }

        if (progress != null && input.Length > 0)
        {
            progress.Report((double)inputIndex / input.Length);
        }

        return outputSamplesWritten;
    }

    private void AnalyzeInto(ReadOnlySpan<float> frame)
    {
        for (int i = 0; i < _frameSize; i++)
            _analysisBuffer[i] = new Complex(frame[i] * _window[i], 0.0);
        Fft.Forward(_analysisBuffer);
    }

    /// <summary>
    /// Returns a short description of the current over-subtraction and floor settings.
    /// </summary>
    public override string ToString() => $"SpectralSubtractor {{ OverSubtractionFactor = {OverSubtractionFactor}, SpectralFloor = {SpectralFloor} }}";
}
