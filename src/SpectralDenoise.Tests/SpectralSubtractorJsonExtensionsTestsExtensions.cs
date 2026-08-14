using System;

namespace SpectralDenoise.Tests;

/// <summary>
/// Extension helpers for <see cref="SpectralSubtractorJsonExtensionsTests"/> that make
/// common JSON‑serialization scenarios easier to use from other test code.
/// </summary>
public static class SpectralSubtractorJsonExtensionsTestsExtensions
{
    /// <summary>
    /// Serializes a <see cref="SpectralSubtractor"/> to JSON using the library's
    /// <c>SpectralSubtractorJsonExtensions.ToJson</c> method.
    /// </summary>
    /// <param name="test">The test instance (required for extension method syntax).</param>
    /// <param name="subtractor">The <see cref="SpectralSubtractor"/> to serialize.</param>
    /// <param name="indented">Whether to produce indented JSON.</param>
    /// <returns>The JSON string representing <paramref name="subtractor"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="test"/> or <paramref name="subtractor"/> is <c>null</c>.
    /// </exception>
    public static string SerializeHappyPath(this SpectralSubtractorJsonExtensionsTests test, SpectralSubtractor subtractor, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(test);
        ArgumentNullException.ThrowIfNull(subtractor);
        return SpectralSubtractorJsonExtensions.ToJson(subtractor, indented);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="SpectralSubtractor"/> and throws if the
    /// result is <c>null</c>. This mirrors the behaviour of the happy‑path test
    /// <c>FromJson_HappyPath_ReturnsSpectralSubtractor</c>.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized <see cref="SpectralSubtractor"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="test"/> or <paramref name="json"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if deserialization returns <c>null</c>.
    /// </exception>
    public static SpectralSubtractor DeserializeOrThrow(this SpectralSubtractorJsonExtensionsTests test, string json)
    {
        ArgumentNullException.ThrowIfNull(test);
        ArgumentException.ThrowIfNullOrEmpty(json);
        var result = SpectralSubtractorJsonExtensions.FromJson(json);
        if (result is null)
            throw new ArgumentException("Deserialization returned null.", nameof(json));
        return result;
    }

    /// <summary>
    /// Attempts to deserialize a JSON string using <c>TryFromJson</c> and returns the
    /// boolean outcome together with the out value.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="json">The JSON string to parse.</param>
    /// <param name="subtractor">
    /// When the method returns <c>true</c>, receives the deserialized <see cref="SpectralSubtractor"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if deserialization succeeded; otherwise <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="test"/> or <paramref name="json"/> is <c>null</c>.
    /// </exception>
    public static bool TryDeserialize(this SpectralSubtractorJsonExtensionsTests test, string json, out SpectralSubtractor? subtractor)
    {
        ArgumentNullException.ThrowIfNull(test);
        ArgumentException.ThrowIfNullOrEmpty(json);
        return SpectralSubtractorJsonExtensions.TryFromJson(json, out subtractor);
    }

    /// <summary>
    /// Performs a full round‑trip: serializes <paramref name="original"/> to JSON and then
    /// deserializes it back, checking that the resulting object is equal to the original.
    /// Equality is based on the overridden <c>Equals</c> implementation of
    /// <see cref="SpectralSubtractor"/>.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="original">The original <see cref="SpectralSubtractor"/> to round‑trip.</param>
    /// <param name="indented">Whether to use indented JSON for the round‑trip.</param>
    /// <returns>
    /// <c>true</c> if the round‑trip yields an equivalent object; otherwise <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="test"/> or <paramref name="original"/> is <c>null</c>.
    /// </exception>
    public static bool VerifyRoundTrip(this SpectralSubtractorJsonExtensionsTests test, SpectralSubtractor original, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(test);
        ArgumentNullException.ThrowIfNull(original);
        var json = SpectralSubtractorJsonExtensions.ToJson(original, indented);
        return SpectralSubtractorJsonExtensions.TryFromJson(json, out var roundTrip) &&
               roundTrip is not null &&
               roundTrip.Equals(original);
    }
}
