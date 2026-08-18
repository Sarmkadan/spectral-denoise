using System;

namespace SpectralDenoise.Tests;

public interface IWavFileTests : IDisposable
{
    void ReadMono_WriteMono_PreservesSampleCountAndRate();
    void ReadMono_WriteMono_PreservesSampleValuesWithinQuantization();
    void ReadMono_HandlesEmptyFileGracefully();
    void ReadMono_WriteMono_WithSingleSample();
    void ReadMono_WriteMono_WithLargeSampleArray();
    void ReadMono_WriteMono_WithClampedValues();
    void WriteMono_HandlesNullSamples();
    void WriteMono_HandlesNegativeSampleRate();
    void WriteMono_HandlesZeroSampleRate();
    void ReadStereo_ReadsTwoChannels();
    void WriteStereo_RejectsDifferentLengthChannels();
    void WriteStereo_HandlesNullLeftChannel();
    void WriteStereo_HandlesNullRightChannel();
    void WriteStereo_HandlesNegativeSampleRate();
    void WriteStereo_HandlesZeroSampleRate();
    void ReadMono_HandlesDifferentSampleRates();
    void RoundTrip_PreservesAudioDataIntegrity();
    void ReadMono_HandlesSilentAudio();
}
