using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace SpectralDenoise;

/// <summary>
/// Converter that always writes doubles with a decimal point (e.g. 1.0 instead of 1).
/// </summary>
internal sealed class DoubleWithDecimalConverter : JsonConverter<double>
{
    public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetDouble();

    public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
    {
        // Use G17 to get full precision, then ensure decimal point
        string text = value.ToString("G17");
        if (!text.Contains('.') && !text.Contains('E') && !text.Contains('e'))
            text += ".0";
        writer.WriteRawValue(text);
    }
}

[JsonSerializable(typeof(SpectralSubtractor))]
public static class SpectralSubtractorJsonExtensions
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = null, // PascalCase
        WriteIndented = false,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        Converters = { new JsonStringEnumConverter(), new DoubleWithDecimalConverter() }
    };

    private static readonly JsonSerializerOptions _optionsIndented = new()
    {
        PropertyNamingPolicy = null, // PascalCase
        WriteIndented = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        Converters = { new JsonStringEnumConverter(), new DoubleWithDecimalConverter() }
    };

    /// <summary>
    /// Serializes a <see cref="SpectralSubtractor"/> instance to a JSON string using culture-invariant formatting.
    /// Sanitizes NaN and Infinity values in noise profiles and other parameters to ensure valid JSON.
    /// </summary>
    /// <param name="value">The instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this SpectralSubtractor value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        // Sanitize the instance to ensure no NaN/Infinity values are serialized
        var sanitized = SanitizeNaNInfinity(value);
        return JsonSerializer.Serialize(sanitized, indented ? _optionsIndented : _options);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="SpectralSubtractor"/> instance.
    /// Validates the deserialized instance using SpectralSubtractorValidation.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized instance, or null if <paramref name="json"/> is null or empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    /// <exception cref="ArgumentException">Thrown when the deserialized instance contains invalid values.</exception>
    public static SpectralSubtractor? FromJson(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        var instance = JsonSerializer.Deserialize<SpectralSubtractor>(json, _options);
        return instance;
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="SpectralSubtractor"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized instance if successful.</param>
    /// <returns>True if deserialization succeeded and validation passed; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public static bool TryFromJson(string? json, out SpectralSubtractor? value)
    {
        value = null;

        if (string.IsNullOrEmpty(json))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<SpectralSubtractor>(json, _options);
            return value is not null;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Sanitizes a SpectralSubtractor instance by replacing NaN and Infinity values with valid defaults.
    /// This ensures valid JSON serialization across different cultures and edge cases.
    /// </summary>
    /// <param name="instance">The instance to sanitize.</param>
    /// <returns>The same instance with sanitized property values (properties are mutable).</returns>
    private static SpectralSubtractor SanitizeNaNInfinity(SpectralSubtractor instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        // Sanitize mutable property values that could contain NaN/Infinity
        if (double.IsNaN(instance.OverSubtractionFactor) || double.IsInfinity(instance.OverSubtractionFactor))
        {
            instance.OverSubtractionFactor = SpectralSubtractorJsonExtensionsConstants.DefaultOverSubtractionFactor;
        }

        if (double.IsNaN(instance.SpectralFloor) || double.IsInfinity(instance.SpectralFloor))
        {
            instance.SpectralFloor = SpectralSubtractorJsonExtensionsConstants.DefaultSpectralFloor;
        }

        if (double.IsNaN(instance.AttackMs) || double.IsInfinity(instance.AttackMs))
        {
            instance.AttackMs = SpectralSubtractorJsonExtensionsConstants.DefaultAttackMs;
        }

        if (double.IsNaN(instance.ReleaseMs) || double.IsInfinity(instance.ReleaseMs))
        {
            instance.ReleaseMs = SpectralSubtractorJsonExtensionsConstants.DefaultReleaseMs;
        }

        // Mode is an enum, can't be NaN/Infinity
        return instance;
    }
}
