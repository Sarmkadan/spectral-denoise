using System.Numerics;

namespace SpectralDenoise;

/// <summary>
/// Plain in-place radix-2 Cooley-Tukey FFT. Length must be a power of two.
/// Nothing fancy - no SIMD, no bit-reversal table caching. Good enough for
/// offline batch processing while I figure out whether the whole approach
/// is even worth keeping.
/// </summary>
public static class Fft
{
    public static void Forward(Complex[] buffer) => Transform(buffer, invert: false);

    public static void Inverse(Complex[] buffer)
    {
        Transform(buffer, invert: true);
        // normalise
        for (int i = 0; i < buffer.Length; i++)
            buffer[i] /= buffer.Length;
    }

    private static void Transform(Complex[] a, bool invert)
    {
        int n = a.Length;
        if ((n & (n - 1)) != 0)
            throw new ArgumentException($"FFT length must be a power of two, got {n}.");

        // bit-reversal permutation
        for (int i = 1, j = 0; i < n; i++)
        {
            int bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1)
                j ^= bit;
            j ^= bit;
            if (i < j)
                (a[i], a[j]) = (a[j], a[i]);
        }

        for (int len = 2; len <= n; len <<= 1)
        {
            double ang = 2 * Math.PI / len * (invert ? 1 : -1);
            var wlen = new Complex(Math.Cos(ang), Math.Sin(ang));
            for (int i = 0; i < n; i += len)
            {
                Complex w = Complex.One;
                for (int k = 0; k < len / 2; k++)
                {
                    Complex u = a[i + k];
                    Complex v = a[i + k + len / 2] * w;
                    a[i + k] = u + v;
                    a[i + k + len / 2] = u - v;
                    w *= wlen;
                }
            }
        }
    }

    public static int NextPow2(int x)
    {
        int p = 1;
        while (p < x) p <<= 1;
        return p;
    }
}
