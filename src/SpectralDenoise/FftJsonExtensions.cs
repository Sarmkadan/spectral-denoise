using System;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpectralDenoise;

/// <summary>
/// Provides System.Text.Json serialization helpers for <see cref="Complex"/> arrays.
/// </summary>
public static class FftJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    private static readonly JsonSerializerOptions _jsonOptionsIndented = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    /// <summary>
    /// Serializes a <see cref="Complex"/> array to a JSON string using culture-invariant formatting.
    /// Replaces NaN and Infinity values with 0 to ensure valid JSON serialization.
    /// </summary>
    /// <param name="value">The <see cref="Complex"/> array to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the array.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this Complex[] value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        // Sanitize NaN and Infinity values to ensure valid JSON serialization
        var sanitized = SanitizeNaNInfinity(value);
        return JsonSerializer.Serialize(sanitized, indented ? _jsonOptionsIndented : _jsonOptions);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="Complex"/> array.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized <see cref="Complex"/> array, or null if the JSON is invalid.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    /// <exception cref="ArgumentException">Thrown when the deserialized array contains invalid values.</exception>
    public static Complex[]? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            var array = JsonSerializer.Deserialize<Complex[]>(json, _jsonOptions);

            // Validate the deserialized array
            if (array is not null)
            {
                array.EnsureValidComplexArray();
            }

            return array;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="Complex"/> array.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized <see cref="Complex"/> array, or null if deserialization fails.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    public static bool TryFromJson(string json, out Complex[]? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            value = JsonSerializer.Deserialize<Complex[]>(json, _jsonOptions);

            // Validate the deserialized array
            if (value is not null)
            {
                value.EnsureValidComplexArray();
            }

            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }

    /// <summary>
    /// Sanitizes a Complex array by replacing NaN and Infinity values with 0.
    /// This ensures valid JSON serialization across different cultures and edge cases.
    /// </summary>
    /// <param name="array">The array to sanitize.</param>
    /// <returns>A new array with sanitized values.</returns>
    private static Complex[] SanitizeNaNInfinity(Complex[] array)
    {
        var result = new Complex[array.Length];
        for (int i = 0; i < array.Length; i++)
        {
            double real = array[i].Real;
            double imaginary = array[i].Imaginary;

            // Replace NaN and Infinity with 0 to ensure valid JSON
            real = double.IsNaN(real) || double.IsInfinity(real) ? 0.0 : real;
            imaginary = double.IsNaN(imaginary) || double.IsInfinity(imaginary) ? 0.0 : imaginary;

            result[i] = new Complex(real, imaginary);
        }

        return result;
    }
}

/// <summary>
/// Provides validation extensions for Complex arrays.
/// </summary>
internal static class ComplexArrayValidation
{
    /// <summary>
    /// Validates that a Complex array is suitable for FFT processing.
    /// Ensures the array is not null, not empty, and all values are valid numbers.
    /// </summary>
    /// <param name="array">The array to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when array is null.</exception>
    /// <exception cref="ArgumentException">Thrown when array is empty or contains invalid values.</exception>
    public static void EnsureValidComplexArray(this Complex[] array)
    {
        ArgumentNullException.ThrowIfNull(array);

        if (array.Length == 0)
        {
            throw new ArgumentException("Complex array cannot be empty.", nameof(array));
        }

        // Validate each element
        for (int i = 0; i < array.Length; i++)
        {
            if (double.IsNaN(array[i].Real) || double.IsInfinity(array[i].Real))
            {
                throw new ArgumentException(
                    ValidationMessages.FormatCollectionError(nameof(array), i,
                    $"must have valid real component (got {array[i].Real})."),
                    nameof(array));
            }

            if (double.IsNaN(array[i].Imaginary) || double.IsInfinity(array[i].Imaginary))
            {
                throw new ArgumentException(
                    ValidationMessages.FormatCollectionError(nameof(array), i,
                    $"must have valid imaginary component (got {array[i].Imaginary})."),
                    nameof(array));
            }
        }
    }
}