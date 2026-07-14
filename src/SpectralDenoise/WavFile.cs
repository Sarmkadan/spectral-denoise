using NAudio.Wave;

namespace SpectralDenoise;

/// <summary>
/// Thin NAudio wrapper. Reads any PCM/IEEE wav into a mono float array and
/// writes 16-bit PCM back out. Stereo gets downmixed to mono because the
/// denoiser only handles a single channel for now (TODO).
/// </summary>
public static class WavFile
{
    public static (float[] samples, int sampleRate) ReadMono(string path)
    {
        using var reader = new AudioFileReader(path);
        int channels = reader.WaveFormat.Channels;
        int sampleRate = reader.WaveFormat.SampleRate;

        var interleaved = new List<float>();
        var buffer = new float[reader.WaveFormat.SampleRate * channels];
        int read;
        while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
            for (int i = 0; i < read; i++)
                interleaved.Add(buffer[i]);

        if (channels == 1)
            return (interleaved.ToArray(), sampleRate);

        int frames = interleaved.Count / channels;
        var mono = new float[frames];
        for (int f = 0; f < frames; f++)
        {
            float sum = 0;
            for (int c = 0; c < channels; c++)
                sum += interleaved[f * channels + c];
            mono[f] = sum / channels;
        }
        return (mono, sampleRate);
    }

    public static void WriteMono(string path, float[] samples, int sampleRate)
    {
        var format = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
        using var writer = new WaveFileWriter(path, format);
        writer.WriteSamples(samples, 0, samples.Length);
    }
}
