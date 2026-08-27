using System;
using System.Numerics;
using SpectralDenoise;
using Xunit;

namespace SpectralDenoise.Tests
{
    /// <summary>
    /// Tests for the <see cref="FftJsonExtensions"/> class.
    /// </summary>
    public class FftJsonExtensionsTests : IFftJsonExtensionsTests
    {
        /// <summary>
        /// Tests that passing a null array to ToJson throws an ArgumentNullException.
        /// </summary>
        [Fact]
        public void ToJson_NullArray_ThrowsArgumentNullException()
        {
            Complex[]? nullArray = null;
            Assert.Throws<ArgumentNullException>(() => FftJsonExtensions.ToJson(nullArray!));
        }

        /// <summary>
        /// Tests that ToJson converts a simple array of complex numbers to the expected JSON string.
        /// </summary>
        [Fact]
        public void ToJson_SimpleArray_ReturnsExpectedJson()
        {
            var data = new Complex[]
            {
                new Complex(1.0, 2.0),
                new Complex(3.5, -4.25)
            };

            string json = data.ToJson();

            // Expected JSON with camelCase property names and no indentation
            const string expected = "[{\"real\":1,\"imaginary\":2},{\"real\":3.5,\"imaginary\":-4.25}]";
            Assert.Equal(expected, json);
        }

        /// <summary>
        /// Tests that ToJson with indented true returns JSON containing a newline character.
        /// </summary>
        [Fact]
        public void ToJson_Indented_ReturnsIndentedJson()
        {
            var data = new Complex[] { new Complex(0, 0) };
            string json = data.ToJson(indented: true);

            // Indented JSON should contain a newline character
            Assert.Contains(Environment.NewLine, json);
        }

        /// <summary>
        /// Tests that FromJson throws an ArgumentException when given null or empty JSON string.
        /// </summary>
        /// <param name="json">The JSON string to parse, which is null or empty.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void FromJson_NullOrEmpty_ThrowsArgumentException(string json)
        {
            Assert.Throws<ArgumentException>(() => FftJsonExtensions.FromJson(json!));
        }

        /// <summary>
        /// Tests that FromJson correctly parses a valid JSON string into an array of complex numbers.
        /// </summary>
        [Fact]
        public void FromJson_ValidJson_ReturnsArray()
        {
            const string json = "[{\"real\":1,\"imaginary\":2},{\"real\":3.5,\"imaginary\":-4.25}]";
            Complex[]? result = FftJsonExtensions.FromJson(json);

            Assert.NotNull(result);
            Assert.Equal(2, result.Length);
            Assert.Equal(new Complex(1, 2), result[0]);
            Assert.Equal(new Complex(3.5, -4.25), result[1]);
        }

        /// <summary>
        /// Tests that FromJson returns null when given an invalid JSON string.
        /// </summary>
        [Fact]
        public void FromJson_InvalidJson_ReturnsNull()
        {
            const string json = "this is not json";
            Complex[]? result = FftJsonExtensions.FromJson(json);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that TryFromJson throws an ArgumentException when given null or empty JSON string.
        /// </summary>
        /// <param name="json">The JSON string to parse, which is null or empty.</param>
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void TryFromJson_NullOrEmpty_ThrowsArgumentException(string json)
        {
            Assert.Throws<ArgumentException>(() => FftJsonExtensions.TryFromJson(json!, out _));
        }

        /// <summary>
        /// Tests that TryFromJson successfully parses a valid JSON string and returns true with the parsed array.
        /// </summary>
        [Fact]
        public void TryFromJson_ValidJson_ReturnsTrueAndArray()
        {
            const string json = "[{\"real\":0,\"imaginary\":0}]";
            bool success = FftJsonExtensions.TryFromJson(json, out Complex[]? result);

            Assert.True(success);
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(new Complex(0, 0), result[0]);
        }

        /// <summary>
        /// Tests that TryFromJson returns false and null when given an invalid JSON string.
        /// </summary>
        [Fact]
        public void TryFromJson_InvalidJson_ReturnsFalseAndNull()
        {
            const string json = "[{invalid json}]";
            bool success = FftJsonExtensions.TryFromJson(json, out Complex[]? result);

            Assert.False(success);
            Assert.Null(result);
        }
    }
}