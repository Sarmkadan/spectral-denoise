using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpectralDenoise.Tests;

/// <summary>
/// JSON serialization helpers for <see cref="NoiseProfileTests"/>.
/// </summary>
public static class NoiseProfileTestsJsonExtensions
{
    /// <summary>
    /// Cached JSON serializer options configured for camelCase property naming.
    /// </summary>
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes a <see cref="NoiseProfileTests"/> instance to JSON.
    /// </summary>
    /// <param name="value">The instance to serialize.</param>
    /// <param name="indented">Whether the output should be indented.</param>
    /// <returns>A JSON string representation of the instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this NoiseProfileTests value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);
        var options = indented
            ? new JsonSerializerOptions(_options) { WriteIndented = true }
            : _options;
        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string into a <see cref="NoiseProfileTests"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized instance, or <c>null</c> if deserialization fails.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public static NoiseProfileTests? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return JsonSerializer.Deserialize<NoiseProfileTests>(json, _options);
    }

    /// <summary>
    /// Tries to deserialize a JSON string into a <see cref="NoiseProfileTests"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">When this method returns, contains the deserialized instance if successful; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if deserialization succeeded; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public static bool TryFromJson(string json, out NoiseProfileTests? value)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            value = JsonSerializer.Deserialize<NoiseProfileTests>(json, _options);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}
