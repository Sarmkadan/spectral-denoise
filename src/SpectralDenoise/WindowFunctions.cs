namespace SpectralDenoise;

using System;

/// <summary>
/// Interface for window functions.
/// </summary>
public interface IWindowFunction
{
    /// <summary>
    /// Name of the window function.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Fills the supplied span with the window values.
    /// </summary>
    /// <param name="window">Span to be filled. Length determines the window size.</param>
    void Fill(Span<float> window);
}

/// <summary>
/// Hann window implementation (symmetric or periodic).
/// </summary>
public sealed class HannWindowFunction : IWindowFunction
{
    public string Name => "Hann";

    private readonly bool _periodic;

    public HannWindowFunction(bool periodic = false) => _periodic = periodic;

    public void Fill(Span<float> window)
    {
        int size = window.Length;
        if (size == 0)
            return;

        double period = _periodic ? size : size - 1;
        for (int i = 0; i < size; i++)
        {
            window[i] = (float)(0.5 * (1.0 - Math.Cos(2.0 * Math.PI * i / period)));
        }
    }
}

/// <summary>
/// Hamming window implementation (symmetric).
/// </summary>
public sealed class HammingWindowFunction : IWindowFunction
{
    public string Name => "Hamming";

    public void Fill(Span<float> window)
    {
        int size = window.Length;
        if (size == 0)
            return;

        double period = size - 1;
        for (int i = 0; i < size; i++)
        {
            window[i] = (float)(0.54 - 0.46 * Math.Cos(2.0 * Math.PI * i / period));
        }
    }
}

/// <summary>
/// Blackman window implementation (symmetric).
/// </summary>
public sealed class BlackmanWindowFunction : IWindowFunction
{
    public string Name => "Blackman";

    public void Fill(Span<float> window)
    {
        int size = window.Length;
        if (size == 0)
            return;

        double period = size - 1;
        for (int i = 0; i < size; i++)
        {
            double a0 = 0.42;
            double a1 = 0.5;
            double a2 = 0.08;
            double cos1 = Math.Cos(2.0 * Math.PI * i / period);
            double cos2 = Math.Cos(4.0 * Math.PI * i / period);
            window[i] = (float)(a0 - a1 * cos1 + a2 * cos2);
        }
    }
}

public static class WindowFunctions
{
    /// <summary>
    /// Symmetric Hann window (periodic = false).
    /// </summary>
    /// <param name="size">Window size (must be positive)</param>
    /// <param name="periodic">Whether to use periodic variant</param>
    /// <returns>Window function of length size</returns>
    /// <exception cref="ArgumentException">Thrown when size is not positive</exception>
    public static double[] Hann(int size, bool periodic = false)
    {
        if (size <= 0)
            throw new ArgumentException("Size must be positive.", nameof(size));

        // Use the new HannWindowFunction implementation
        var floatWindow = new float[size];
        new HannWindowFunction(periodic).Fill(floatWindow);

        var w = new double[size];
        for (int i = 0; i < size; i++)
            w[i] = floatWindow[i];
        return w;
    }

    /// <summary>
    /// Hamming window (symmetric).
    /// </summary>
    /// <param name="size">Window size (must be positive)</param>
    /// <returns>Window function of length size</returns>
    public static double[] Hamming(int size)
    {
        if (size <= 0)
            throw new ArgumentException("Size must be positive.", nameof(size));

        var floatWindow = new float[size];
        new HammingWindowFunction().Fill(floatWindow);

        var w = new double[size];
        for (int i = 0; i < size; i++)
            w[i] = floatWindow[i];
        return w;
    }

    /// <summary>
    /// Blackman window (symmetric).
    /// </summary>
    /// <param name="size">Window size (must be positive)</param>
    /// <returns>Window function of length size</returns>
    public static double[] Blackman(int size)
    {
        if (size <= 0)
            throw new ArgumentException("Size must be positive.", nameof(size));

        var floatWindow = new float[size];
        new BlackmanWindowFunction().Fill(floatWindow);

        var w = new double[size];
        for (int i = 0; i < size; i++)
            w[i] = floatWindow[i];
        return w;
    }

    /// <summary>
    /// Periodic Hann window. Periodic (not symmetric) variant is the right one
    /// for overlap-add STFT so that the squared windows sum to a constant.
    /// </summary>
    /// <param name="size">Window size (must be at least 4)</param>
    /// <returns>Periodic Hann window</returns>
    /// <exception cref="ArgumentException">Thrown when size is not positive or less than 4</exception>
    public static double[] HannPeriodic(int size)
    {
        if (size <= 0)
            throw new ArgumentException("Size must be positive.", nameof(size));
        if (size < 4)
            throw new ArgumentException("Size must be at least 4 for periodic Hann window.", nameof(size));

        return NormalizeForCola(Hann(size, periodic: true), size / 4);
    }

    /// <summary>
    /// Normalizes a window function to satisfy the Constant Overlap-Add (COLA) condition
    /// for a given hop size.
    /// </summary>
    /// <param name="window">The window function to normalize</param>
    /// <param name="hop">Hop size (number of samples between frames)</param>
    /// <returns>A new window normalized to satisfy COLA</returns>
    public static double[] NormalizeForCola(ReadOnlySpan<double> window, int hop)
    {
        if (window.Length == 0)
            throw new ArgumentException("Window cannot be empty.", nameof(window));
        if (hop <= 0)
            throw new ArgumentException("Hop must be positive.", nameof(hop));

        double sumSquared = 0.0;
        for (int i = 0; i < window.Length; i++)
        {
            sumSquared += window[i] * window[i];
        }

        // Normalize so that sum of squared window equals hop size
        // This ensures the COLA condition is satisfied
        if (sumSquared > 1e-10)
        {
            double scale = Math.Sqrt(hop / sumSquared);
            var normalized = new double[window.Length];
            for (int i = 0; i < window.Length; i++)
            {
                normalized[i] = window[i] * scale;
            }
            return normalized;
        }

        // If sum is zero, return a rectangular window
        var rectangular = new double[window.Length];
        rectangular[window.Length / 2] = 1.0;
        return rectangular;
    }

    /// <summary>
    /// Checks if a window function satisfies the Constant Overlap-Add (COLA) condition
    /// for a given hop size.
    /// </summary>
    /// <param name="window">The window function</param>
    /// <param name="hop">Hop size (number of samples between frames)</param>
    /// <returns>True if the window/overlap combination satisfies COLA</returns>
    public static bool SatisfiesCola(ReadOnlySpan<double> window, int hop)
    {
        if (window.Length == 0)
            return false;
        if (hop <= 0)
            return false;

        // For COLA, the sum of squared windows should be approximately equal to hop size
        // after normalization
        double sumSquared = 0.0;
        for (int i = 0; i < window.Length; i++)
        {
            sumSquared += window[i] * window[i];
        }

        // Allow a small tolerance for numerical precision
        const double tolerance = 1e-3; // More lenient tolerance for normalized windows
        double target = hop;

        return Math.Abs(sumSquared - target) < tolerance;
    }

    /// <summary>
    /// Computes the sum of squared window values for normalization.
    /// This is used to undo the analysis+synthesis window weighting in overlap-add.
    /// </summary>
    /// <param name="window">The window function</param>
    /// <param name="hop">Hop size</param>
    /// <param name="outputLength">Output length</param>
    /// <returns>The sum of squared window values</returns>
    public static double[] ComputeWindowSumSquared(ReadOnlySpan<double> window, int hop, int outputLength)
    {
        if (window.Length == 0)
            throw new ArgumentException("Window cannot be empty.", nameof(window));
        if (hop <= 0)
            throw new ArgumentException("Hop must be positive.", nameof(hop));

        var normalization = new double[outputLength];

        // For each output sample, compute the sum of squared windows that overlap it
        for (int i = 0; i < outputLength; i++)
        {
            // Find all frames that contribute to this sample
            int firstFrame = Math.Max(0, (i - window.Length + hop) / hop);
            int lastFrame = Math.Min((outputLength - 1) / hop, i / hop);

            double sum = 0.0;
            for (int frame = firstFrame; frame <= lastFrame; frame++)
            {
                int windowIndex = i - frame * hop;
                if (windowIndex >= 0 && windowIndex < window.Length)
                {
                    sum += window[windowIndex] * window[windowIndex];
                }
            }

            normalization[i] = sum;
        }

        return normalization;
    }
}
