using System;
using System.Text.Json;

namespace SpectralDenoise.Tests;

/// <summary>
/// Provides JSON serialization and deserialization extensions for <see cref="SpectralSubtractorValidationTests"/>.
/// </summary>
public static class SpectralSubtractorValidationTestsJsonExtensions
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Converts the <see cref="SpectralSubtractorValidationTests"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="indented">If true, writes the JSON with indentation; otherwise, writes compact JSON.</param>
    /// <returns>A JSON string representation of the value.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static string ToJson(this SpectralSubtractorValidationTests value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(Options) { WriteIndented = true }
            : Options;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Parses the JSON string into a <see cref="SpectralSubtractorValidationTests"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <returns>A <see cref="SpectralSubtractorValidationTests"/> instance, or null if deserialization fails.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="json"/> is null or empty.</exception>
    public static SpectralSubtractorValidationTests? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        return JsonSerializer.Deserialize<SpectralSubtractorValidationTests>(json, Options);
    }

    /// <summary>
    /// Tries to parse the JSON string into a <see cref="SpectralSubtractorValidationTests"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <param name="value">When this method returns, contains the deserialized instance if successful; otherwise, null.</param>
    /// <returns>true if the JSON was parsed successfully; otherwise, false.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="json"/> is null or empty.</exception>
    public static bool TryFromJson(string json, out SpectralSubtractorValidationTests? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            value = JsonSerializer.Deserialize<SpectralSubtractorValidationTests>(json, Options);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}
