using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpectralDenoise;

/// <summary>
/// Provides System.Text.Json serialization helpers for <see cref="NoiseProfile"/>.
/// </summary>
public static class NoiseProfileJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    /// <summary>
    /// Serializes a <see cref="NoiseProfile"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The <see cref="NoiseProfile"/> to serialize.</param>
    /// <param name="indented">If <c>true</c>, the output JSON is formatted with indentation.</param>
    /// <returns>A JSON representation of the <paramref name="value"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
    public static string ToJson(this NoiseProfile value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        // Use a copy of the cached options when indentation is requested to avoid mutating the shared instance.
        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="NoiseProfile"/> instance.
    /// </summary>
    /// <param name="json">The JSON string representing a <see cref="NoiseProfile"/>.</param>
    /// <returns>The deserialized <see cref="NoiseProfile"/>, or <c>null</c> if the JSON is invalid.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is <c>null</c> or empty.</exception>
    public static NoiseProfile? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            return JsonSerializer.Deserialize<NoiseProfile>(json, _jsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="NoiseProfile"/> instance.
    /// </summary>
    /// <param name="json">The JSON string representing a <see cref="NoiseProfile"/>.</param>
    /// <param name="value">When this method returns, contains the deserialized <see cref="NoiseProfile"/> if the operation succeeded; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if deserialization succeeded; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is <c>null</c> or empty.</exception>
    public static bool TryFromJson(string json, out NoiseProfile? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            value = JsonSerializer.Deserialize<NoiseProfile>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}
