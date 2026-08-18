using System;
using System.Numerics;

namespace SpectralDenoise.Tests
{
    public interface IFftJsonExtensionsTests
    {
        void ToJson_NullArray_ThrowsArgumentNullException();
        void ToJson_SimpleArray_ReturnsExpectedJson();
        void ToJson_Indented_ReturnsIndentedJson();
        void FromJson_NullOrEmpty_ThrowsArgumentException(string json);
        void FromJson_ValidJson_ReturnsArray();
        void FromJson_InvalidJson_ReturnsNull();
        void TryFromJson_NullOrEmpty_ThrowsArgumentException(string json);
        void TryFromJson_ValidJson_ReturnsTrueAndArray();
        void TryFromJson_InvalidJson_ReturnsFalseAndNull();
    }
}
