namespace SpectralDenoise;

internal static class WindowFunctions
{
    /// <summary>
    /// Periodic Hann window. Periodic (not symmetric) variant is the right one
    /// for overlap-add STFT so that the squared windows sum to a constant.
    /// </summary>
    public static double[] Hann(int size)
    {
        var w = new double[size];
        for (int i = 0; i < size; i++)
            w[i] = 0.5 * (1.0 - Math.Cos(2.0 * Math.PI * i / size));
        return w;
    }
}
