using System;
using System.Numerics;
using SpectralDenoise;
using Xunit;

namespace SpectralDenoise.Tests
{
    public class FftJsonExtensionsTests
    {
        [Fact]
        public void ToJson_NullArray_ThrowsArgumentNullException()
        {
            Complex[]? nullArray = null;
            Assert.Throws<ArgumentNullException>(() => FftJsonExtensions.ToJson(nullArray!));
        }

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

        [Fact]
        public void ToJson_Indented_ReturnsIndentedJson()
        {
            var data = new Complex[] { new Complex(0, 0) };
            string json = data.ToJson(indented: true);

            // Indented JSON should contain a newline character
            Assert.Contains(Environment.NewLine, json);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void FromJson_NullOrEmpty_ThrowsArgumentException(string json)
        {
            Assert.Throws<ArgumentException>(() => FftJsonExtensions.FromJson(json!));
        }

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

        [Fact]
        public void FromJson_InvalidJson_ReturnsNull()
        {
            const string json = "this is not json";
            Complex[]? result = FftJsonExtensions.FromJson(json);
            Assert.Null(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void TryFromJson_NullOrEmpty_ThrowsArgumentException(string json)
        {
            Assert.Throws<ArgumentException>(() => FftJsonExtensions.TryFromJson(json!, out _));
        }

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
