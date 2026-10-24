using System;

namespace SpectralDenoise;

/// <summary>
/// Finds the stretch of a signal with the lowest energy, which is the most likely noise-only region.
/// The signal is split into hop-sized blocks, and the run of contiguous blocks with the smallest
/// total energy is chosen. Ties go to the earliest run, so a uniformly noisy clip keeps the leading
/// segment. A clip too short to hold a full window also falls back to the leading segment.
/// </summary>
public sealed class NoiseSegmentDetector : INoiseSegmentDetector
{
    private readonly int _hop;

    /// <summary>
    /// Initializes a new instance of the <see cref="NoiseSegmentDetector"/> class.
    /// </summary>
    /// <param name="hop">Block size in samples. Use the hop of the <see cref="SpectralSubtractor"/> that will estimate the profile.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when hop is not positive.</exception>
    public NoiseSegmentDetector(int hop)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(hop);

        _hop = hop;
    }

    /// <summary>
    /// Gets the block size in samples used when measuring energy.
    /// </summary>
    public int Hop => _hop;

    /// <summary>
    /// Returns the leading segment of a signal, without any energy analysis.
    /// </summary>
    /// <param name="signal">Mono samples to take the segment from.</param>
    /// <param name="sampleRate">Audio sample rate in Hz.</param>
    /// <param name="seconds">Desired length of the segment in seconds. Clamped to the signal length.</param>
    /// <returns>A segment starting at sample zero.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when sampleRate is not positive or seconds is not a finite positive number.</exception>
    /// <exception cref="ArgumentException">Thrown when signal is empty.</exception>
    public static NoiseSegment Leading(ReadOnlySpan<float> signal, int sampleRate, double seconds)
        => new(0, WindowLength(signal, sampleRate, seconds));

    /// <summary>
    /// Finds the lowest-energy segment of a mono signal, falling back to the leading segment when the
    /// signal is shorter than one window.
    /// </summary>
    /// <param name="signal">Mono samples to search.</param>
    /// <param name="sampleRate">Audio sample rate in Hz.</param>
    /// <param name="seconds">Desired length of the noise segment in seconds.</param>
    /// <returns>The segment with the lowest summed energy, or the leading segment when no search is possible.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when sampleRate is not positive or seconds is not a finite positive number.</exception>
    /// <exception cref="ArgumentException">Thrown when signal is empty.</exception>
    public NoiseSegment Detect(ReadOnlySpan<float> signal, int sampleRate, double seconds)
    {
        int length = WindowLength(signal, sampleRate, seconds);
        int blocksPerWindow = (length + _hop - 1) / _hop;
        int blockCount = signal.Length / _hop;

        if (length == 0 || blocksPerWindow > blockCount)
            return new NoiseSegment(0, length);

        double[] prefix = BlockEnergyPrefixSums(signal, blockCount);

        int bestStart = 0;
        double bestEnergy = double.PositiveInfinity;
        for (int start = 0; start + blocksPerWindow <= blockCount; start++)
        {
            double energy = prefix[start + blocksPerWindow] - prefix[start];
            if (energy < bestEnergy)
            {
                bestEnergy = energy;
                bestStart = start;
            }
        }

        // Every candidate window ends within blockCount * _hop <= signal.Length, so the
        // requested length fits after bestStart * _hop.
        return new NoiseSegment(bestStart * _hop, length);
    }

    /// <summary>
    /// Validates the arguments and converts the requested duration to a sample count, clamped to the signal.
    /// </summary>
    private static int WindowLength(ReadOnlySpan<float> signal, int sampleRate, double seconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(sampleRate, 0);

        if (!double.IsFinite(seconds) || seconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(seconds), NoiseSegmentDetectorConstants.ErrorSecondsPositive);

        if (signal.IsEmpty)
            throw new ArgumentException(NoiseSegmentDetectorConstants.ErrorSignalEmpty, nameof(signal));

        return (int)Math.Min(signal.Length, Math.Round(sampleRate * seconds));
    }

    /// <summary>
    /// Builds prefix sums of per-block energy, so any run of blocks can be summed in constant time.
    /// </summary>
    /// <returns>An array of length <paramref name="blockCount"/> + 1 where element i is the energy of the first i blocks.</returns>
    private double[] BlockEnergyPrefixSums(ReadOnlySpan<float> signal, int blockCount)
    {
        var prefix = new double[blockCount + 1];

        for (int block = 0; block < blockCount; block++)
        {
            double energy = 0.0;
            foreach (float sample in signal.Slice(block * _hop, _hop))
                energy += (double)sample * sample;

            prefix[block + 1] = prefix[block] + energy;
        }

        return prefix;
    }
}
