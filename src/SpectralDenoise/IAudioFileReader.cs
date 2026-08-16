using System;
using System.Collections.Generic;

namespace SpectralDenoise;

public interface IAudioFileReader
{
    IEnumerable<(float[] samples, int sampleRate, bool isLastBlock)> ReadMonoStream(string path, int blockSize = 8192, IProgress<double>? progress = null);
    (float[] samples, int sampleRate) ReadMono(string path);
    (float[] left, float[] right, int sampleRate) ReadStereo(string path);
}
