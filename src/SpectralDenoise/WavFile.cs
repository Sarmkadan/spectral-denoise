using System;
using System.Collections.Generic;
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

    public static (float[] left, float[] right, int sampleRate) ReadStereo(string path)
    {
        using var reader = new AudioFileReader(path);
        if (reader.WaveFormat.Channels != 2)
            throw new InvalidDataException("Input file must be stereo (2 channels).");

        int sampleRate = reader.WaveFormat.SampleRate;
        var interleaved = new List<float>();
        var buffer = new float[reader.WaveFormat.SampleRate * reader.WaveFormat.Channels];
        int read;
        while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
            for (int i = 0; i < read; i++)
                interleaved.Add(buffer[i]);

        int frames = interleaved.Count / 2;
        var left = new float[frames];
        var right = new float[frames];

        for (int f = 0; f < frames; f++)
        {
            left[f] = interleaved[f * 2];
            right[f] = interleaved[f * 2 + 1];
        }

        return (left, right, sampleRate);
    }

    public static void WriteStereo(string path, float[] left, float[] right, int sampleRate)
    {
        if (left.Length != right.Length)
            throw new InvalidDataException("Left and right channel arrays must have the same length.");

        // 16-bit PCM Stereo
        var format = new WaveFormat(sampleRate, 16, 2);
        using var writer = new WaveFileWriter(path, format);

        int frameCount = left.Length;
        var interleavedShorts = new short[frameCount * 2];

        for (int i = 0; i < frameCount; i++)
        {
            // Clamp and convert float (-1.0 to 1.0) to short (-32768 to 32767)
            float l = Math.Max(-1.0f, Math.Min(1.0f, left[i]));
            float r = Math.Max(-1.0f, Math.Min(1.0f, right[i]));

            interleavedShorts[i * 2] = (short)(l * 32767f);
            interleavedShorts[i * 2 + 1] = (short)(r * 32767f);
        }

        writer.WriteSamples(interleavedShorts, 0, interleavedShorts.Length);
    }
}
