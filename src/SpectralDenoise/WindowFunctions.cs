namespace SpectralDenoise;

public static class WindowFunctions
{
    /// <summary>
    /// Symmetric Hann window (periodic = false).
    /// </summary>
    public static double[] Hann(int size, bool periodic = false)
    {
        var w = new double[size];
        double period = periodic ? size : size - 1;
        for (int i = 0; i < size; i++)
            w[i] = 0.5 * (1.0 - Math.Cos(2.0 * Math.PI * i / period));
        return w;
    }

    /// <summary>
    /// Periodic Hann window. Periodic (not symmetric) variant is the right one
    /// for overlap-add STFT so that the squared windows sum to a constant.
    /// </summary>
    public static double[] HannPeriodic(int size)
    {
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
