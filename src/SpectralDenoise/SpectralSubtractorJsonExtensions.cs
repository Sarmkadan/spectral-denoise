using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace SpectralDenoise;

[JsonSerializable(typeof(SpectralSubtractor))]
public static class SpectralSubtractorJsonExtensions
{
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Serializes a <see cref="SpectralSubtractor"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this SpectralSubtractor value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);
        return JsonSerializer.Serialize(value, indented ? new JsonSerializerOptions(_options) { WriteIndented = true } : _options);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="SpectralSubtractor"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized instance, or null if <paramref name="json"/> is null or empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static SpectralSubtractor? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return string.IsNullOrEmpty(json)
            ? null
            : JsonSerializer.Deserialize<SpectralSubtractor>(json, _options);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="SpectralSubtractor"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized instance if successful.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public static bool TryFromJson(string json, out SpectralSubtractor? value)
    {
        ArgumentNullException.ThrowIfNull(json);
        value = string.IsNullOrEmpty(json)
            ? null
            : JsonSerializer.Deserialize<SpectralSubtractor>(json, _options);
        return value is not null;
    }
}