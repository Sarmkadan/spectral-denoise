using System.Numerics;

namespace SpectralDenoise;

/// <summary>
/// Plain in-place radix-2 Cooley-Tukey FFT.
/// Length must be a power of two.
/// Nothing fancy - no SIMD, no bit-reversal table caching. Good enough for
/// offline batch processing while I figure out whether the whole approach
/// is even worth keeping.
/// </summary>
public static class Fft
{
    /// <summary>
    /// Computes the forward FFT (frequency spectrum) in-place.
    /// </summary>
    /// <param name="buffer">Input signal to transform. Must have a power-of-two length.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="buffer"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="buffer"/>.Length is not a power of two.</exception>
    public static void Forward(Complex[] buffer)
        => Transform(buffer, invert: false);

    /// <summary>
    /// Computes the inverse FFT (time-domain signal) in-place and normalizes by 1/N.
    /// </summary>
    /// <param name="buffer">Frequency-domain signal to transform back to time domain. Must have a power-of-two length.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="buffer"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="buffer"/>.Length is not a power of two.</exception>
    public static void Inverse(Complex[] buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        Transform(buffer, invert: true);

        // Normalise
        for (int i = 0; i < buffer.Length; i++)
            buffer[i] /= buffer.Length;
    }

    /// <summary>
    /// Radix-2 Cooley-Tukey FFT algorithm.
    /// </summary>
    /// <param name="a">Input/output buffer. Must have a power-of-two length.</param>
    /// <param name="invert">Whether to compute forward (false) or inverse (true) transform.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="a"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="a"/>.Length is not a power of two.</exception>
    private static void Transform(Complex[] a, bool invert)
    {
        ArgumentNullException.ThrowIfNull(a);

        int n = a.Length;
        if (n == 0)
            throw new ArgumentException("Buffer cannot be empty.", nameof(a));

        if ((n & (n - 1)) != 0)
            throw new ArgumentException($"FFT length must be a power of two, got {n}.");

        // Bit-reversal permutation
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

    /// <summary>
    /// Computes the smallest power of two greater than or equal to <paramref name="x"/>.
    /// </summary>
    /// <param name="x">Input value.</param>
    /// <returns>The smallest power of two ≥ x.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="x"/> is negative.</exception>
    public static int NextPow2(int x)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(x);

        int p = 1;
        while (p < x)
            p <<= 1;

        return p;
    }
}
