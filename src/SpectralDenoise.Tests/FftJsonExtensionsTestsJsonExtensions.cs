using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpectralDenoise.Tests;

/// <summary>
/// Provides System.Text.Json serialization helpers for <see cref="FftJsonExtensionsTests"/>.
/// </summary>
public static class FftJsonExtensionsTestsJsonExtensions
{
    // Single cached options with camel‑case naming; WriteIndented is set per call.
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    /// <summary>
    /// Serializes a <see cref="FftJsonExtensionsTests"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation.</param>
    /// <returns>A JSON representation of <paramref name="value"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
    public static string ToJson(this FftJsonExtensionsTests value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);
        _jsonOptions.WriteIndented = indented;
        return JsonSerializer.Serialize(value, _jsonOptions);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="FftJsonExtensionsTests"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized instance, or <c>null</c> if the JSON is invalid.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is <c>null</c> or empty.</exception>
    public static FftJsonExtensionsTests? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);
        try
        {
            return JsonSerializer.Deserialize<FftJsonExtensionsTests>(json, _jsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="FftJsonExtensionsTests"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">
    /// When this method returns, contains the deserialized <see cref="FftJsonExtensionsTests"/>
    /// instance if the operation succeeded; otherwise, <c>null</c>.
    /// </param>
    /// <returns><c>true</c> if deserialization succeeded; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is <c>null</c> or empty.</exception>
    public static bool TryFromJson(string json, out FftJsonExtensionsTests? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);
        try
        {
            value = JsonSerializer.Deserialize<FftJsonExtensionsTests>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}
