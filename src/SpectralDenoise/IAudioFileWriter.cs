namespace SpectralDenoise;

public interface IAudioFileWriter
{
    void WriteMono(string path, float[] samples, int sampleRate);
    void WriteStereo(string path, float[] left, float[] right, int sampleRate);
}
