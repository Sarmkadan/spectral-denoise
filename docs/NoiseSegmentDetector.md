# NoiseSegmentDetector

Finds the part of a clip that is most likely noise-only, so that `SpectralSubtractor.EstimateNoiseProfile` can build a noise profile from it. The clip is split into hop-sized blocks, and the run of contiguous blocks with the lowest summed energy is chosen. This replaces the fixed "first 0.5 s is noise" assumption, which fails when a recording starts with speech.

## API

### `public NoiseSegmentDetector(int hop)`
Creates a detector that measures energy in blocks of `hop` samples. Pass the hop of the `SpectralSubtractor` that will estimate the profile.
- `hop`: block size in samples; must be positive.

Throws `ArgumentOutOfRangeException` if `hop` is not positive.

### `public int Hop`
Gets the block size in samples used when measuring energy.

### `public NoiseSegment Detect(ReadOnlySpan<float> signal, int sampleRate, double seconds)`
Returns the lowest-energy segment of `signal` whose length is `seconds` worth of samples, clamped to the signal length.
- `signal`: mono samples; must not be empty.
- `sampleRate`: sample rate in Hz; must be positive.
- `seconds`: desired segment length; must be a finite number greater than zero.

The segment with the smallest summed energy wins. Ties go to the earliest segment, so a uniformly noisy clip keeps the leading segment. If the clip is too short to hold a full window, the leading segment is returned.

Throws `ArgumentOutOfRangeException` if `sampleRate` is not positive or `seconds` is not a finite positive number.
Throws `ArgumentException` if `signal` is empty.

### `public static NoiseSegment Leading(ReadOnlySpan<float> signal, int sampleRate, double seconds)`
Returns the first `seconds` of `signal`, clamped to the signal length, without any energy analysis. Use it to keep the old behaviour.

Throws `ArgumentOutOfRangeException` if `sampleRate` is not positive or `seconds` is not a finite positive number.
Throws `ArgumentException` if `signal` is empty.

## Usage

```csharp
using SpectralDenoise;

var (samples, sampleRate) = WavFile.ReadMono("input.wav");
var subtractor = new SpectralSubtractor(frameSize: 1024, hop: 256);

var detector = new NoiseSegmentDetector(subtractor.Hop);
NoiseSegment noise = detector.Detect(samples, sampleRate, seconds: 0.5);

double[] profile = subtractor.EstimateNoiseProfile(samples.AsSpan(noise.Start, noise.Length));
```

## Notes

- Energy is the sum of squared sample values per block. There is no spectral weighting and no speech model, so a quiet consonant or breath can win over a truly silent gap.
- A segment shorter than one frame still makes `EstimateNoiseProfile` throw, as it did before.
- Digital silence has zero energy and wins the search. Its noise profile is all zeros, so subtraction removes nothing.
