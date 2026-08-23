using System;
using System.Collections.Generic;
using System.Buffers.Binary;
using System.IO;
using System.Text;
using NAudio.Wave;

namespace SpectralDenoise;

/// <summary>
/// Provides methods for reading and writing WAV files in various formats.
/// Supports PCM (16-bit) and IEEE float (32-bit) WAV files with sample rates from 8000 Hz to 192000 Hz and 1 to 8 channels.
/// </summary>
    /// <summary>
    /// Provides methods for reading and writing WAV files in various formats.
    /// Supports PCM (16-bit) and IEEE float (32-bit) WAV files with sample rates from 8000 Hz to 192000 Hz and 1 to 8 channels.
    /// </summary>
        /// <summary>
    /// Provides methods for reading and writing WAV files in various formats.
    /// Supports PCM (16-bit) and IEEE float (32-bit) WAV files with sample rates from 8000 Hz to 192000 Hz and 1 to 8 channels.
    /// </summary>
    public class WavFile : IAudioFileReader, IAudioFileWriter
{
    private const int MaxSampleRate = 192_000;
    private const int MinSampleRate = 8_000;
    private const int MaxChannels = 8;
    private const long MaxAllowedSamples = 100_000_000; // ~400MB of floats, reasonable limit

    /// <summary>
    /// Maximum allowed file size for reading (100MB)
    /// </summary>
    private const long MaxFileSize = 100_000_000;

    /// <summary>
    /// Maximum allowed data chunk size in bytes (100MB)
    /// </summary>
    private const int MaxDataChunkSize = 100_000_000;

    /// <summary>
    /// Parses and validates a WAV header using a single read of the file into memory.
    /// Validation is performed via BinaryPrimitives over a ReadOnlySpan&lt;byte&gt;.
    /// </summary>
    /// <param name="path">Path to the WAV file</param>
    /// <exception cref="ArgumentNullException">If path is null.</exception>
    /// <exception cref="InvalidDataException">If the header is malformed or violates constraints.</exception>
    private static void ParseAndValidateHeader(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        // Normalize and validate the path to prevent path traversal attacks
        string normalizedPath = Path.GetFullPath(path);
        string currentDir = Path.GetFullPath(".");
        string rootDir = Path.GetFullPath("/");

        if (!normalizedPath.StartsWith(currentDir, StringComparison.Ordinal) &&
            !normalizedPath.StartsWith(rootDir, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Path must be within the current directory or a subdirectory. Path: {path}",
                nameof(path));
        }

        var fileInfo = new FileInfo(path);
        if (!fileInfo.Exists)
            throw new FileNotFoundException("The specified WAV file was not found.", path);

        if (fileInfo.Length > MaxFileSize)
            throw new InvalidDataException("File size exceeds maximum allowed limit of 100MB.");

        // Read the entire file into memory once
        byte[] fileBytes = File.ReadAllBytes(path);
        ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(fileBytes);

        if (span.Length < 44)
            throw new InvalidDataException("File too small to be a valid WAV file.");

        int offset = 0;

        // RIFF header
        if (Encoding.ASCII.GetString(span.Slice(offset, 4)) != "RIFF")
            throw new InvalidDataException("Missing RIFF header.");
        offset += 4;

        // Skip overall file size (4 bytes)
        offset += 4;

        if (Encoding.ASCII.GetString(span.Slice(offset, 4)) != "WAVE")
            throw new InvalidDataException("Missing WAVE header.");
        offset += 4;

        bool fmtFound = false;
        bool dataFound = false;

        int audioFormat = 0;
        int channels = 0;
        int sampleRate = 0;
        int bitsPerSample = 0;
        long dataChunkSize = 0;

        while (offset + 8 <= span.Length)
        {
            string chunkId = Encoding.ASCII.GetString(span.Slice(offset, 4));
            offset += 4;

            int chunkSize = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(offset, 4));
            offset += 4;

            if (chunkId == "fmt ")
            {
                fmtFound = true;
                if (chunkSize < 16)
                    throw new InvalidDataException("Invalid fmt chunk size.");

                audioFormat = BinaryPrimitives.ReadInt16LittleEndian(span.Slice(offset, 2));
                channels = BinaryPrimitives.ReadInt16LittleEndian(span.Slice(offset + 2, 2));
                sampleRate = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(offset + 4, 4));
                // byteRate (4) and blockAlign (2) are skipped
                bitsPerSample = BinaryPrimitives.ReadInt16LittleEndian(span.Slice(offset + 14, 2));
            }
            else if (chunkId == "data")
            {
                dataFound = true;
                dataChunkSize = chunkSize;
                // No need to parse further for validation
            }

            // Move to next chunk (including padding byte if chunk size is odd)
            offset += chunkSize;
            if ((chunkSize & 1) == 1)
                offset += 1;
        }

        if (!fmtFound)
            throw new InvalidDataException("fmt chunk not found.");
        if (!dataFound)
            throw new InvalidDataException("data chunk not found.");

        // Validate sample rate
        if (sampleRate < MinSampleRate || sampleRate > MaxSampleRate)
            throw new InvalidDataException(
                $"Unsupported sample rate: {sampleRate}. Must be between {MinSampleRate}Hz and {MaxSampleRate}Hz.");

        // Validate channel count
        if (channels < 1 || channels > MaxChannels)
            throw new InvalidDataException(
                $"Unsupported channel count: {channels}. Must be between 1 and {MaxChannels} channels.");

        // Validate audio format (1 = PCM, 3 = IEEE float)
        if (audioFormat != 1 && audioFormat != 3)
            throw new InvalidDataException(
                $"Unsupported WAV format code: {audioFormat}. Only PCM (1) and IEEE float (3) are supported.");

        // Validate bits per sample (commonly 16 for PCM, 32 for float)
        if (bitsPerSample != 16 && bitsPerSample != 32)
            throw new InvalidDataException(
                $"Unsupported bits per sample: {bitsPerSample}. Only 16 or 32 bits are supported.");

        // Validate data chunk size
        if (dataChunkSize > MaxDataChunkSize)
            throw new InvalidDataException("WAV data chunk size exceeds maximum allowed limit of 100MB.");

        // Validate total sample count
        long totalSamples = dataChunkSize / (bitsPerSample / 8);
        if (totalSamples > MaxAllowedSamples)
            throw new InvalidDataException(
                $"File is too large. Maximum allowed samples is {MaxAllowedSamples:N0} ({MaxAllowedSamples * sizeof(float):N0} bytes).");
    }

    /// <summary>
    /// Safely creates an AudioFileReader and validates the WAV file.
    /// Any low‑level parsing errors (truncated header, missing chunks, etc.) are
    /// wrapped in an InvalidDataException with a clear message, preventing
    /// IndexOutOfRangeException from bubbling up.
    /// </summary>
    private static AudioFileReader CreateReader(string path)
    {
        try
        {
            // Perform fast header validation using a single read
            ParseAndValidateHeader(path);

            // If validation passes, let NAudio handle the actual reading
            var reader = new AudioFileReader(path);
            return reader;
        }
        catch (Exception ex) when (
            ex is EndOfStreamException ||
            ex is IndexOutOfRangeException ||
            ex is InvalidDataException ||
            ex is ArgumentException ||
            ex is IOException)
        {
            // Wrap low‑level parsing errors in a consistent InvalidDataException
            throw new InvalidDataException($"Failed to read WAV file '{path}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Reads audio from a WAV file in blocks, allowing streaming processing of large files.
    /// </summary>
    /// <param name="path">Path to the WAV file</param>
    /// <param name="blockSize">Block size in samples (default: 8192)</param>
    /// <param name="progress">Optional progress reporter</param>
    /// <returns>An enumerable that yields audio blocks as they are read</returns>
    /// <exception cref="ArgumentNullException">Thrown if path is null.</exception>
    /// <exception cref="ArgumentException">Thrown if path is empty or contains invalid characters.</exception>
    /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
    /// <exception cref="InvalidDataException">Thrown if WAV header is invalid or file is malformed.</exception>
    public static IEnumerable<(float[] samples, int sampleRate, bool isLastBlock)> ReadMonoStream(string path, int blockSize = 8192, IProgress<double>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(path);

        using var reader = CreateReader(path);

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
            int totalFramesInFile = (int)(reader.Length / sizeof(float) / reader.WaveFormat.Channels);

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

                bool isLastBlock = (totalFrames + framesRead) >= totalFramesInFile;
                int actualFrames = isLastBlock ? framesRead : blockSize;
                yield return (monoBuffer.AsSpan(0, actualFrames).ToArray(), sampleRate, isLastBlock);
                totalRead += actualFrames;
                totalFrames += framesRead;

                if (progress != null && totalFramesInFile > 0)
                {
                    progress.Report((double)totalFrames / totalFramesInFile);
                }
            }
        }
    }

    IEnumerable<(float[] samples, int sampleRate, bool isLastBlock)> IAudioFileReader.ReadMonoStream(string path, int blockSize, IProgress<double>? progress) => ReadMonoStream(path, blockSize, progress);

    /// <summary>
    /// Reads the entire audio from a WAV file as a mono stream.
    /// </summary>
    /// <param name="path">Path to the WAV file</param>
    /// <returns>An audio sample array and the sample rate.</returns>
    /// <exception cref="ArgumentNullException">Thrown if path is null.</exception>
    /// <exception cref="ArgumentException">Thrown if path is empty or contains invalid characters.</exception>
    /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
    /// <exception cref="InvalidDataException">Thrown if WAV header is invalid or file is malformed.</exception>
    public static (float[] samples, int sampleRate) ReadMono(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        using var reader = CreateReader(path);

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
        // Limit buffer size to avoid massive allocation based on potentially malicious header
        int channels = reader.WaveFormat.Channels;
        var buffer = new float[Math.Min(reader.WaveFormat.SampleRate * channels, 100_000)];

        int read;
        while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
            for (int i = 0; i < read; i++)
                interleaved.Add(buffer[i]);

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

    (float[] samples, int sampleRate) IAudioFileReader.ReadMono(string path) => ReadMono(path);

    public static void WriteMono(string path, float[] samples, int sampleRate)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(samples);

        var format = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
        using var writer = new WaveFileWriter(path, format);
        writer.WriteSamples(samples, 0, samples.Length);
    }

    void IAudioFileWriter.WriteMono(string path, float[] samples, int sampleRate) => WriteMono(path, samples, sampleRate);

    /// <summary>
    /// Reads the entire audio from a WAV file as a stereo stream.
    /// </summary>
    /// <param name="path">Path to the WAV file</param>
    /// <returns>Left and right sample arrays and the sample rate.</returns>
    /// <exception cref="ArgumentNullException">Thrown if path is null.</exception>
    /// <exception cref="ArgumentException">Thrown if path is empty or contains invalid characters.</exception>
    /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
    /// <exception cref="InvalidDataException">Thrown if WAV header is invalid, file is not stereo, or file is malformed.</exception>
    public static (float[] left, float[] right, int sampleRate) ReadStereo(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        using var reader = CreateReader(path);

        if (reader.WaveFormat.Channels != 2)
            throw new InvalidDataException("Input file must be stereo (2 channels).");

        int sampleRate = reader.WaveFormat.SampleRate;
        var interleaved = new List<float>();
        // Limit buffer size
        var buffer = new float[Math.Min(reader.WaveFormat.SampleRate * 2, 100_000)];

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

    (float[] left, float[] right, int sampleRate) IAudioFileReader.ReadStereo(string path) => ReadStereo(path);

    public static void WriteStereo(string path, float[] left, float[] right, int sampleRate)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

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

    void IAudioFileWriter.WriteStereo(string path, float[] left, float[] right, int sampleRate) => WriteStereo(path, left, right, sampleRate);
}
