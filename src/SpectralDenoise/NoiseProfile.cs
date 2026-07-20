using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace SpectralDenoise;

/// <summary>
/// Represents a noise profile that can be serialized to/from JSON.
/// Contains the noise magnitude spectrum along with metadata about the audio processing.
/// </summary>
public sealed class NoiseProfile
{
    /// <summary>
    /// Gets the noise magnitude spectrum (one value per frequency bin).
    /// </summary>
    [JsonInclude]
    public double[] Magnitudes { get; private set; } = Array.Empty<double>();

    /// <summary>
    /// Gets the sample rate in Hz.
    /// </summary>
    [JsonInclude]
    public int SampleRate { get; private set; }

    /// <summary>
    /// Gets the FFT frame size (number of samples per analysis frame).
    /// </summary>
    [JsonInclude]
    public int FrameSize { get; private set; }

    /// <summary>
    /// Gets the hop size (number of samples between analysis frames).
    /// </summary>
    [JsonInclude]
    public int Hop { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NoiseProfile"/> class.
    /// </summary>
    /// <param name="magnitudes">The noise magnitude spectrum.</param>
    /// <param name="sampleRate">The audio sample rate in Hz.</param>
    /// <param name="frameSize">The FFT frame size.</param>
    /// <param name="hop">The hop size between frames.</param>
    /// <exception cref="ArgumentNullException">Thrown when magnitudes is null.</exception>
    /// <exception cref="ArgumentException">Thrown when magnitudes is empty or sampleRate/hop are not positive.</exception>
    public NoiseProfile(double[] magnitudes, int sampleRate, int frameSize, int hop)
    {
        ArgumentNullException.ThrowIfNull(magnitudes);

        if (magnitudes.Length == 0)
            throw new ArgumentException("Magnitudes array cannot be empty.", nameof(magnitudes));

        if (sampleRate <= 0)
            throw new ArgumentException("Sample rate must be positive.", nameof(sampleRate));

        if (frameSize <= 0)
            throw new ArgumentException("Frame size must be positive.", nameof(frameSize));

        if (hop <= 0)
            throw new ArgumentException("Hop must be positive.", nameof(hop));

        Magnitudes = magnitudes;
        SampleRate = sampleRate;
        FrameSize = frameSize;
        Hop = hop;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NoiseProfile"/> class.
    /// This constructor is for JSON deserialization only.
    /// </summary>
    [JsonConstructor]
    private NoiseProfile() { }

    /// <summary>
    /// Creates a noise profile from a SpectralSubtractor's noise estimation.
    /// </summary>
    /// <param name="magnitudes">The noise magnitude spectrum.</param>
    /// <param name="sampleRate">The audio sample rate in Hz.</param>
    /// <param name="subtractor">The SpectralSubtractor instance used for processing.</param>
    /// <returns>A new NoiseProfile instance.</returns>
    public static NoiseProfile FromEstimate(double[] magnitudes, int sampleRate, SpectralSubtractor subtractor)
    {
        ArgumentNullException.ThrowIfNull(magnitudes);
        ArgumentNullException.ThrowIfNull(subtractor);

        return new NoiseProfile(magnitudes, sampleRate, subtractor.FrameSize, subtractor.Hop);
    }

    /// <summary>
    /// Serializes this noise profile to a JSON string.
    /// </summary>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the noise profile.</returns>
    /// <exception cref="ArgumentNullException">Thrown when this instance is null.</exception>
    public string ToJson(bool indented = false)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = indented,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return JsonSerializer.Serialize(this, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a NoiseProfile instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized NoiseProfile instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when json is null.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static NoiseProfile FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        return JsonSerializer.Deserialize<NoiseProfile>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        }) ?? throw new JsonException("Deserialization returned null.");
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a NoiseProfile instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="profile">Receives the deserialized instance if successful.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when json is null.</exception>
    public static bool TryFromJson(string json, out NoiseProfile? profile)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            profile = JsonSerializer.Deserialize<NoiseProfile>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            });
            return profile is not null;
        }
        catch
        {
            profile = null;
            return false;
        }
    }

    /// <summary>
    /// Validates that the deserialized noise profile matches the expected processing parameters.
    /// </summary>
    /// <param name="sampleRate">The expected sample rate.</param>
    /// <param name="frameSize">The expected frame size.</param>
    /// <param name="hop">The expected hop size.</param>
    /// <exception cref="InvalidOperationException">Thrown when validation fails.</exception>
    public void Validate(int sampleRate, int frameSize, int hop)
    {
        if (SampleRate != sampleRate)
            throw new InvalidOperationException(
                $"Sample rate mismatch: expected {sampleRate}Hz, got {SampleRate}Hz");

        if (FrameSize != frameSize)
            throw new InvalidOperationException(
                $"Frame size mismatch: expected {frameSize}, got {FrameSize}");

        if (Hop != hop)
            throw new InvalidOperationException(
                $"Hop size mismatch: expected {hop}, got {Hop}");

        if (Magnitudes.Length != frameSize / 2 + 1)
            throw new InvalidOperationException(
                $"Magnitude array length mismatch: expected {frameSize / 2 + 1}, got {Magnitudes.Length}");
    }

    /// <summary>
    /// Validates that the deserialized noise profile can be used with a SpectralSubtractor.
    /// </summary>
    /// <param name="subtractor">The SpectralSubtractor instance to validate against.</param>
    /// <exception cref="InvalidOperationException">Thrown when validation fails.</exception>
    public void Validate(SpectralSubtractor subtractor)
    {
        ArgumentNullException.ThrowIfNull(subtractor);
        Validate(subtractor.FrameSize, subtractor.FrameSize, subtractor.Hop);
    }
}