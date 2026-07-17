using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace SpectralDenoise;

/// <summary>
/// Provides JSON serialization and deserialization extensions for <see cref="SpectralSubtractor"/>.
/// </summary>
public static class SpectralSubtractorJsonExtensions
{
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
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

        var options = indented
            ? new JsonSerializerOptions(_options) { WriteIndented = true }
            : _options;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="SpectralSubtractor"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized instance, or null if the JSON is null or empty.</returns>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static SpectralSubtractor? FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        return JsonSerializer.Deserialize<SpectralSubtractor>(json, _options);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="SpectralSubtractor"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized instance if successful.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    public static bool TryFromJson(string json, out SpectralSubtractor? value)
    {
        value = null;
        if (string.IsNullOrEmpty(json))
            return false;

        try
        {
            value = JsonSerializer.Deserialize<SpectralSubtractor>(json, _options);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}