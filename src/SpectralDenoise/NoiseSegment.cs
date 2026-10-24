namespace SpectralDenoise;

/// <summary>
/// A contiguous span of samples inside a signal, assumed to contain only noise.
/// </summary>
/// <param name="Start">Index of the first sample of the segment within the signal.</param>
/// <param name="Length">Number of samples in the segment.</param>
public readonly record struct NoiseSegment(int Start, int Length);
