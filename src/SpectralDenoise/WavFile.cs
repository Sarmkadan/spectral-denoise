using System;
using System.Collections.Generic;
using NAudio.Wave;

namespace SpectralDenoise;

/// <summary>
/// Thin NAudio wrapper. Reads any PCM/IEEE wav into float arrays and
/// writes 16-bit PCM back out. Supports mono and stereo channels.
/// </summary>
public static class WavFile
{
    /// <summary>
    /// Reads audio from a WAV file in blocks, allowing streaming processing of large files.
    /// </summary>
    /// <param name="path">Path to the WAV file</param>
    /// <param name="blockSize">Block size in samples (default: 8192)</param>
    /// <param name="progress">Optional progress reporter</param>
    /// <returns>An enumerable that yields audio blocks as they are read</returns>
    public static IEnumerable<(float[] samples, int sampleRate, bool isLastBlock)> ReadMonoStream(string path, int blockSize = 8192, IProgress<double>? progress = null)
    {
        using var reader = new AudioFileReader(path);
        int sampleRate = reader.WaveFormat.SampleRate;

        // For mono files, read in blocks
        if (reader.WaveFormat.Channels == 1)
        {
            var buffer = new float[blockSize];
            int read;
            int totalRead = 0;
            int totalSamples = (int)reader.Length / sizeof(float);

            while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
            {
                bool isLastBlock = (totalRead + read) >= totalSamples;
                yield return (buffer.AsSpan(0, read).ToArray(), sampleRate, isLastBlock);
                totalRead += read;

                if (progress != null && totalSamples > 0)
                {
                    progress.Report((double)totalRead / totalSamples);
                }
            }
        }
        else
        {
            // For multi-channel files, read and downmix to mono in blocks
            var interleavedBuffer = new float[blockSize * reader.WaveFormat.Channels];
            var monoBuffer = new float[blockSize];
            int read;
            int totalRead = 0;
            int totalFrames = 0;

            while ((read = reader.Read(interleavedBuffer, 0, interleavedBuffer.Length)) > 0)
            {
                int framesRead = read / reader.WaveFormat.Channels;
                for (int f = 0; f < framesRead; f++)
                {
                    float sum = 0;
                    for (int c = 0; c < reader.WaveFormat.Channels; c++)
                    {
                        sum += interleavedBuffer[f * reader.WaveFormat.Channels + c];
                    }
                    monoBuffer[f] = sum / reader.WaveFormat.Channels;
                }

                bool isLastBlock = (totalFrames + framesRead) * reader.WaveFormat.Channels >= reader.Length / sizeof(float);
                int actualFrames = isLastBlock ? framesRead : blockSize;
                yield return (monoBuffer.AsSpan(0, actualFrames).ToArray(), sampleRate, isLastBlock);
                totalRead += actualFrames;
                totalFrames += framesRead;

                if (progress != null && reader.Length > 0)
                {
                    progress.Report((double)totalRead / (reader.Length / sizeof(float) / reader.WaveFormat.Channels));
                }
            }
        }
    }

    public static (float[] samples, int sampleRate) ReadMono(string path)
    {
        using var reader = new AudioFileReader(path);
        int sampleRate = reader.WaveFormat.SampleRate;

        // For mono files, read directly
        if (reader.WaveFormat.Channels == 1)
        {
            var samples = new float[reader.Length / sizeof(float)];
            reader.Read(samples, 0, samples.Length);
            return (samples, sampleRate);
        }

        // For multi-channel files, read and downmix to mono
        var interleaved = new List<float>();
        var buffer = new float[reader.WaveFormat.SampleRate * reader.WaveFormat.Channels];
        int read;
        while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
            for (int i = 0; i < read; i++)
                interleaved.Add(buffer[i]);

        int frames = interleaved.Count / reader.WaveFormat.Channels;
        var mono = new float[frames];
        for (int f = 0; f < frames; f++)
        {
            float sum = 0;
            for (int c = 0; c < reader.WaveFormat.Channels; c++)
                sum += interleaved[f * reader.WaveFormat.Channels + c];
            mono[f] = sum / reader.WaveFormat.Channels;
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