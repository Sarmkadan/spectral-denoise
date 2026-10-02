using System;
using System.Numerics;
using System.Collections.Generic;

namespace SpectralDenoise;

/// <summary>
/// Plain in-place radix-2 Cooley-Tukey FFT with caching of bit-reversal and twiddle factors.
/// Length must be a power of two.
/// </summary>
public static class Fft
{
    private const int MaxLength = 1 << 20; // 2^20

    // Cache for bit-reversal arrays (key: length, value: bit-reversal indices)
    private static readonly Dictionary<int, int[]> _bitReversalCache = new();
    // Cache for twiddle factors (key: length, value: TwiddleFactors containing forward and inverse arrays)
    private static readonly Dictionary<int, TwiddleFactors> _twiddleCache = new();
    private static readonly object _cacheLock = new();

    /// <summary>
    /// Computes the forward FFT (frequency spectrum) in-place.
    /// </summary>
    /// <param name="buffer">Input signal to transform. Must have a power-of-two length.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="buffer"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="buffer"/>.Length is not a power of two or exceeds the maximum length.</exception>
    public static void Forward(Complex[] buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        if (buffer.Length < 1)
            throw new ArgumentException("Buffer length must be at least 1.", nameof(buffer));
        if ((buffer.Length & (buffer.Length - 1)) != 0)
            throw new ArgumentException($"FFT length must be a power of two, got {buffer.Length}.", nameof(buffer));
        if (buffer.Length > MaxLength)
            throw new ArgumentException($"FFT length exceeds the maximum length of {MaxLength}.", nameof(buffer));

        // Length 1 is a trivial case - no transformation needed
        if (buffer.Length == 1)
            return;

        Forward(buffer.AsSpan());
    }

    /// <summary>
    /// Computes the inverse FFT (time-domain signal) in-place and normalizes by 1/N.
    /// </summary>
    /// <param name="buffer">Frequency-domain signal to transform back to time domain. Must have a power-of-two length.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="buffer"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="buffer"/>.Length is not a power of two or exceeds the maximum length.</exception>
    public static void Inverse(Complex[] buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        if (buffer.Length < 1)
            throw new ArgumentException("Buffer length must be at least 1.", nameof(buffer));
        if ((buffer.Length & (buffer.Length - 1)) != 0)
            throw new ArgumentException($"FFT length must be a power of two, got {buffer.Length}.", nameof(buffer));
        if (buffer.Length > MaxLength)
            throw new ArgumentException($"FFT length exceeds the maximum length of {MaxLength}.", nameof(buffer));

        // Length 1 is a trivial case - no transformation needed
        if (buffer.Length == 1)
            return;

        Inverse(buffer.AsSpan());

        // Normalise
        for (int i = 0; i < buffer.Length; i++)
            buffer[i] /= buffer.Length;
    }

    /// <summary>
    /// Computes the forward FFT (frequency spectrum) in-place using a span.
    /// </summary>
    /// <param name="buffer">Input signal to transform. Must have a power-of-two length.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="buffer"/>.Length is not a power of two or exceeds the maximum length.</exception>
    public static void Forward(Span<Complex> buffer)
    {
        int n = buffer.Length;
        if (n < 1)
            throw new ArgumentException("Buffer length must be at least 1.", nameof(buffer));
        if ((n & (n - 1)) != 0)
            throw new ArgumentException($"FFT length must be a power of two, got {n}.", nameof(buffer));
        if (n > MaxLength)
            throw new ArgumentException($"FFT length exceeds the maximum length of {MaxLength}.", nameof(buffer));

        // Length 1 is a trivial case - no transformation needed
        if (n == 1)
            return;

        // Bit-reversal permutation using cached indices
        int[] bitReversal = GetBitReversal(n);
        for (int i = 0; i < n; i++)
        {
            int j = bitReversal[i];
            if (i < j)
            {
                Complex temp = buffer[i];
                buffer[i] = buffer[j];
                buffer[j] = temp;
            }
        }

        // Get cached twiddle factors for this length
        TwiddleFactors twiddle = GetTwiddleFactors(n);

        // Iterative FFT loops
        for (int len = 2; len <= n; len <<= 1)
        {
            int stride = n / len; // stride into twiddle arrays
            for (int i = 0; i < n; i += len)
            {
                for (int j = 0; j < len / 2; j++)
                {
                    Complex u = buffer[i + j];
                    Complex v = buffer[i + j + len / 2] * twiddle.Forward[j * stride];
                    buffer[i + j] = u + v;
                    buffer[i + j + len / 2] = u - v;
                }
            }
        }
    }

    /// <summary>
    /// Computes the inverse FFT (time-domain signal) in-place using a span.
    /// </summary>
    /// <param name="buffer">Frequency-domain signal to transform back to time domain. Must have a power-of-two length.</param>
    public static void Inverse(Span<Complex> buffer)
    {
        int n = buffer.Length;
        if (n < 1)
            throw new ArgumentException("Buffer length must be at least 1.", nameof(buffer));
        if ((n & (n - 1)) != 0)
            throw new ArgumentException($"FFT length must be a power of two, got {n}.", nameof(buffer));
        if (n > MaxLength)
            throw new ArgumentException($"FFT length exceeds the maximum length of {MaxLength}.", nameof(buffer));

        // Length 1 is a trivial case - no transformation needed
        if (n == 1)
            return;

        // Bit-reversal permutation using cached indices
        int[] bitReversal = GetBitReversal(n);
        for (int i = 0; i < n; i++)
        {
            int j = bitReversal[i];
            if (i < j)
            {
                Complex temp = buffer[i];
                buffer[i] = buffer[j];
                buffer[j] = temp;
            }
        }

        // Get cached twiddle factors for this length
        TwiddleFactors twiddle = GetTwiddleFactors(n);

        // Iterative FFT loops (using inverse twiddle factors)
        for (int len = 2; len <= n; len <<= 1)
        {
            int stride = n / len; // stride into twiddle arrays
            for (int i = 0; i < n; i += len)
            {
                for (int j = 0; j < len / 2; j++)
                {
                    Complex u = buffer[i + j];
                    Complex v = buffer[i + j + len / 2] * twiddle.Inverse[j * stride];
                    buffer[i + j] = u + v;
                    buffer[i + j + len / 2] = u - v;
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

    /// <summary>
    /// Gets the bit-reversal indices for the given length (power of two), computing and caching if necessary.
    /// </summary>
    private static int[] GetBitReversal(int n)
    {
        if (_bitReversalCache.TryGetValue(n, out var bitReversal))
            return bitReversal;

        lock (_cacheLock)
        {
            if (_bitReversalCache.TryGetValue(n, out bitReversal))
                return bitReversal;

            bitReversal = new int[n];
            int log2n = (int)Math.Log(n, 2);
            for (int i = 0; i < n; i++)
            {
                int rev = 0;
                for (int j = 0; j < log2n; j++)
                    rev = (rev << 1) | ((i >> j) & 1);
                bitReversal[i] = rev;
            }
            _bitReversalCache[n] = bitReversal;
            return bitReversal;
        }
    }

    /// <summary>
    /// Gets the twiddle factors for the given length (power of two), computing and caching if necessary.
    /// </summary>
    private static TwiddleFactors GetTwiddleFactors(int n)
    {
        if (_twiddleCache.TryGetValue(n, out var twiddle))
            return twiddle;

        lock (_cacheLock)
        {
            if (_twiddleCache.TryGetValue(n, out twiddle))
                return twiddle;

            var forward = new Complex[n / 2];
            var inverse = new Complex[n / 2];
            for (int k = 0; k < n / 2; k++)
            {
                double angle = -2 * Math.PI * k / n; // forward angle
                forward[k] = new Complex(Math.Cos(angle), Math.Sin(angle));
                inverse[k] = Complex.Conjugate(forward[k]); // inverse is conjugate
            }
            twiddle = new TwiddleFactors { Forward = forward, Inverse = inverse };
            _twiddleCache[n] = twiddle;
            return twiddle;
        }
    }

    /// <summary>
    /// Helper structure to hold forward and inverse twiddle factor arrays.
    /// </summary>
    private struct TwiddleFactors
    {
        public Complex[] Forward;
        public Complex[] Inverse;
    }
}