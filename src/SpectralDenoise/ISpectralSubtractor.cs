using System;
using System.Numerics;

namespace SpectralDenoise;

/// <summary>
/// Interface exposing the public members of <see cref="SpectralSubtractor"/>.
/// </summary>
public interface ISpectralSubtractor
{
    /// <summary>
    /// Over‑subtraction factor. 1.0 = plain Boll. Higher = more aggressive.
    /// </summary>
    double Alpha { get; set; }

    /// <summary>
    /// Spectral floor. Keeps a fraction of the original magnitude to mask musical noise.
    /// </summary>
    double Beta { get; set; }

    /// <summary>
    /// Over‑subtraction factor applied to the noise profile during subtraction.
    /// </summary>
    double OverSubtractionFactor { get; set; }

    /// <summary>
    /// Spectral floor. Minimum fraction of the original magnitude kept.
    /// </summary>
    double SpectralFloor { get; set; }

    /// <summary>
    /// Denoising mode: SpectralSubtraction (classic) or Wiener (Wiener filter).
    /// </summary>
    DenoiseMode Mode { get; set; }

    /// <summary>
    /// Attack time in milliseconds for gain smoothing.
    /// </summary>
    double AttackMs { get; set; }

    /// <summary>
    /// Release time in milliseconds for gain smoothing.
    /// </summary>
    double ReleaseMs { get; set; }

    /// <summary>
    /// Resets the smoothing state (previous gain values).
    /// </summary>
    void ResetSmoothing();

    /// <summary>
    /// Estimates a noise magnitude profile from a mono sample span, assumed to be noise‑only.
    /// </summary>
    /// <param name="noiseOnly">Mono samples containing only noise.</param>
    /// <returns>Noise magnitude profile.</returns>
    double[] EstimateNoiseProfile(ReadOnlySpan<float> noiseOnly);

    /// <summary>
    /// Processes an audio signal with the given sample rate.
    /// </summary>
    /// <param name="samples">Input audio signal.</param>
    /// <param name="sampleRate">Audio sample rate in Hz.</param>
    /// <returns>Processed audio signal.</returns>
    float[] Process(float[] samples, int sampleRate);
}
