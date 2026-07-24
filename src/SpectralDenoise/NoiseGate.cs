namespace SpectralDenoise;

/// <summary>
/// A time-domain noise gate that complements spectral subtraction with configurable attack and release smoothing.
///
/// The gate applies exponential envelope smoothing to prevent audible clicks at gate transitions.
/// When the input signal level is below the threshold (in dB), the gain is smoothly reduced to 0.
/// When above threshold, the gain is smoothly increased to 1.
/// </summary>
public sealed class NoiseGate : IAudioProcessor
{
    private readonly float _thresholdDb;
    private readonly float _attackCoeff;
    private readonly float _releaseCoeff;
    private float _currentGain = 1.0f;

    /// <summary>
    /// Creates a new noise gate.
    /// </summary>
    /// <param name="sampleRate">Audio sample rate in Hz (must be positive)</param>
    /// <param name="thresholdDb">Threshold in dB below which the gate closes (default: -45 dB)</param>
    /// <param name="attackMs">Attack time in milliseconds (time to open the gate, default: 5 ms).
    /// Must be in range [0.1, 1000] ms.</param>
    /// <param name="releaseMs">Release time in milliseconds (time to close the gate, default: 100 ms).
    /// Must be in range [1, 5000] ms.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when sampleRate is not positive or attackMs/releaseMs are out of range.</exception>
    public NoiseGate(int sampleRate, float thresholdDb = -45f, float attackMs = 5f, float releaseMs = 100f)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(sampleRate, 0);

        ArgumentOutOfRangeException.ThrowIfLessThan(attackMs, 0.1f);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(attackMs, 1000f);

        ArgumentOutOfRangeException.ThrowIfLessThan(releaseMs, 1f);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(releaseMs, 5000f);

        _thresholdDb = thresholdDb;

        // Convert times to coefficients
        // attack/release are exponential smoothing coefficients
        _attackCoeff = CalculateCoefficient(attackMs, sampleRate);
        _releaseCoeff = CalculateCoefficient(releaseMs, sampleRate);
    }

    /// <summary>
    /// Process a signal through the noise gate.
    /// </summary>
    /// <param name="samples">Input audio signal</param>
    /// <param name="sampleRate">Audio sample rate in Hz</param>
    /// <returns>Gated output signal</returns>
    /// <exception cref="ArgumentNullException">Thrown when samples is null.</exception>
    /// <exception cref="ArgumentException">Thrown when samples is empty or sampleRate is not positive.</exception>
    public float[] Process(float[] samples, int sampleRate)
    {
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(sampleRate, 0);
        ArgumentOutOfRangeException.ThrowIfEqual(samples.Length, 0);

        var output = new float[samples.Length];

        for (int i = 0; i < samples.Length; i++)
        {
            // Convert threshold from dB to linear amplitude
            float thresholdLinear = DecibelsToLinear(_thresholdDb);

            // Get current sample
            float sample = samples[i];
            float absSample = Math.Abs(sample);

            // Calculate desired gain based on current level
            float desiredGain = absSample > thresholdLinear ? 1.0f : 0.0f;

            // Apply attack/release smoothing
            // Use faster coefficient based on whether we're opening or closing
            float coeff = desiredGain > _currentGain ? _attackCoeff : _releaseCoeff;
            _currentGain = desiredGain + _currentGain * (1.0f - coeff);

            // Apply gain to output
            output[i] = sample * _currentGain;
        }

        return output;
    }

    /// <summary>
    /// Reset the gate state (useful between files/chunks).
    /// </summary>
    public void Reset()
    {
        _currentGain = 1.0f;
    }

    /// <summary>
    /// Calculate smoothing coefficient from time in milliseconds.
    /// </summary>
    /// <param name="timeMs">Time constant in milliseconds</param>
    /// <param name="sampleRate">Audio sample rate in Hz</param>
    /// <returns>Exponential smoothing coefficient</returns>
    private static float CalculateCoefficient(float timeMs, int sampleRate)
    {
        // Exponential smoothing: coeff = exp(-1.0 / (time * sampleRate / 1000))
        // For small times, use a reasonable minimum to avoid instability
        float timeSeconds = Math.Max(0.001f, timeMs / 1000.0f);
        return (float)Math.Exp(-1.0f / (timeSeconds * sampleRate));
    }

    /// <summary>
    /// Convert decibels to linear amplitude.
    /// </summary>
    /// <param name="db">Decibel value</param>
    /// <returns>Linear amplitude</returns>
    private static float DecibelsToLinear(float db)
    {
        return (float)Math.Pow(10.0, db / 20.0);
    }
}
