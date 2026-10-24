using System;
using System.Collections.Generic;

namespace SpectralDenoise;

/// <summary>
/// Reads audio files into float sample buffers.
/// </summary>
public interface IAudioFileReader
{
    /// <summary>
    /// Streams a file as mono blocks.
    /// </summary>
    /// <param name="path">Path to the WAV file.</param>
    /// <param name="blockSize">Number of samples per block.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <returns>Mono sample blocks with their sample rate and an end-of-file flag.</returns>
    IEnumerable<(float[] samples, int sampleRate, bool isLastBlock)> ReadMonoStream(string path, int blockSize = 8192, IProgress<double>? progress = null);

    /// <summary>
    /// Reads the whole file as mono.
    /// </summary>
    /// <param name="path">Path to the WAV file.</param>
    /// <returns>Mono samples and their sample rate.</returns>
    (float[] samples, int sampleRate) ReadMono(string path);

    /// <summary>
    /// Reads the whole file as stereo.
    /// </summary>
    /// <param name="path">Path to the WAV file.</param>
    /// <returns>Left and right channel samples and their sample rate.</returns>
    (float[] left, float[] right, int sampleRate) ReadStereo(string path);
}
