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
    /// preventing musical‑noise zeros. Default = 0.01.
    /// </summary>
    public double SpectralFloor { get; set; } = 0.01;

    /// <summary>
    /// Denoising mode: SpectralSubtraction (classic) or Wiener (Wiener filter).
    /// Default = SpectralSubtraction (maintains backward compatibility).
    /// </summary>
    public DenoiseMode Mode { get; init; } = DenoiseMode.SpectralSubtraction;

    /// <summary>
    /// Gets the frame size (number of samples per analysis frame).
    /// </summary>
    public int FrameSize => _frameSize;

    /// <summary>
    /// Gets the hop size (number of samples between analysis frames).
    /// </summary>
    public int Hop => _hop;

    /// <summary>
    /// Gets the analysis window function.
    /// </summary>
    public ReadOnlySpan<double> Window => _window;

    public SpectralSubtractor(int frameSize = 1024, int hop = 256)
    {
        if ((frameSize & (frameSize - 1)) != 0)
            throw new ArgumentException("frameSize must be a power of two.", nameof(frameSize));
        _frameSize = frameSize;
        _hop = hop;
        _window = WindowFunctions.Hann(frameSize);
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
    public float[] Process(ReadOnlySpan<float> signal, double[] noiseProfile)
    {
        int bins = _frameSize / 2 + 1;
        if (noiseProfile.Length != bins)
            throw new ArgumentException("Noise profile bin count does not match frame size.");

        var output = new float[signal.Length];
        var normalisation = new float[signal.Length];

        for (int start = 0; start + _frameSize <= signal.Length; start += _hop)
        {
            var spec = Analyze(signal.Slice(start, _frameSize));

            // Apply denoising based on mode
            for (int b = 0; b < bins; b++)
            {
                double mag = spec[b].Magnitude;
                double phase = spec[b].Phase;

                double cleaned;

                if (Mode == DenoiseMode.Wiener)
                {
                    // Wiener filter: gain = SNR / (SNR + 1)
                    // where SNR = signal_power / noise_power
                    double signalPower = mag * mag;
                    double noisePower = noiseProfile[b] * noiseProfile[b];

                    // Avoid division by zero and negative SNR
                    double snr = signalPower > 1e-20 ? signalPower / noisePower : 0.0;
                    double gain = snr / (snr + 1.0);

                    cleaned = mag * gain;
                }
                else
                {
                    // Classic spectral subtraction
                    cleaned = mag - OverSubtractionFactor * noiseProfile[b];
                }

                // Apply spectral floor
                double floor = SpectralFloor * mag;
                if (cleaned < floor) cleaned = floor;

                spec[b] = Complex.FromPolarCoordinates(cleaned, phase);
                if (b > 0 && b < bins - 1)
                    spec[_frameSize - b] = Complex.Conjugate(spec[b]);
            }

            Fft.Inverse(spec);

            for (int i = 0; i < _frameSize; i++)
            {
                output[start + i] += (float)(spec[i].Real * _window[i]);
                normalisation[start + i] += (float)(_window[i] * _window[i]);
            }
        }

        // undo the analysis+synthesis window weighting
        for (int i = 0; i < output.Length; i++)
            if (normalisation[i] > 1e-6f)
                output[i] /= normalisation[i];

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
