using System;
using System.Text.Json;

namespace SpectralDenoise.Tests;

/// <summary>
/// JSON (de)serialization helpers for <see cref="FftTests"/>.
/// </summary>
public static class FftTestsJsonExtensions
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes the <see cref="FftTests"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation.</param>
    /// <returns>A JSON representation of <paramref name="value"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
    public static string ToJson(this FftTests value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);
        _options.WriteIndented = indented;
        return JsonSerializer.Serialize(value, _options);
    }

    /// <summary>
    /// Deserializes a JSON string to an <see cref="FftTests"/> instance.
    /// </summary>
    /// <param name="json">The JSON string.</param>
    /// <returns>The deserialized <see cref="FftTests"/> instance, or <c>null</c> if the JSON represents a null value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <c>null</c>.</exception>
    /// <exception cref="JsonException">Thrown when the JSON cannot be deserialized to <see cref="FftTests"/>.</exception>
    public static FftTests? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return JsonSerializer.Deserialize<FftTests>(json, _options);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to an <see cref="FftTests"/> instance.
    /// </summary>
    /// <param name="json">The JSON string.</param>
    /// <param name="value">When this method returns, contains the deserialized <see cref="FftTests"/> instance if the operation succeeded; otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if deserialization succeeded; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <c>null</c>.</exception>
    public static bool TryFromJson(string json, out FftTests? value)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            value = JsonSerializer.Deserialize<FftTests>(json, _options);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}
