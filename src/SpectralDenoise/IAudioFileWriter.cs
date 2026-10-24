namespace SpectralDenoise;

/// <summary>
/// Writes float sample buffers to audio files.
/// </summary>
public interface IAudioFileWriter
{
    /// <summary>
    /// Writes a mono buffer to a WAV file.
    /// </summary>
    /// <param name="path">Destination file path.</param>
    /// <param name="samples">Mono samples to write.</param>
    /// <param name="sampleRate">Sample rate in Hz.</param>
    void WriteMono(string path, float[] samples, int sampleRate);

    /// <summary>
    /// Writes a stereo buffer to a WAV file.
    /// </summary>
    /// <param name="path">Destination file path.</param>
    /// <param name="left">Left channel samples.</param>
    /// <param name="right">Right channel samples.</param>
    /// <param name="sampleRate">Sample rate in Hz.</param>
    void WriteStereo(string path, float[] left, float[] right, int sampleRate);
}
