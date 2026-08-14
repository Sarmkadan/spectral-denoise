using System;
using System.Text.Json;

namespace SpectralDenoise.Tests;

public static class WavFileTestsJsonExtensions
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Converts the <see cref="WavFileTests"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The instance to convert.</param>
    /// <param name="indented">If true, writes the JSON with indentation.</param>
    /// <returns>A JSON string representation of the instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    public static string ToJson(this WavFileTests value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true }
            : Options;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Parses a JSON string into a <see cref="WavFileTests"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <returns>The parsed instance, or null if parsing fails.</returns>
    /// <exception cref="ArgumentException">Thrown if json is null or empty.</exception>
    public static WavFileTests? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);
        return JsonSerializer.Deserialize<WavFileTests>(json, Options);
    }

    /// <summary>
    /// Tries to parse a JSON string into a <see cref="WavFileTests"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <param name="value">The parsed instance, or null if parsing fails.</param>
    /// <returns>True if parsing succeeded; otherwise, false.</returns>
    /// <exception cref="ArgumentException">Thrown if json is null or empty.</exception>
    public static bool TryFromJson(string json, out WavFileTests? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);
        try
        {
            value = JsonSerializer.Deserialize<WavFileTests>(json, Options);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}
