# spectral-denoise

Weekend experiment: can plain old **spectral subtraction** clean up hiss on
voice recordings well enough to be useful, without reaching for a neural model?

Stack is just NAudio for the wav I/O plus a hand-rolled radix-2 FFT. No external
DSP dependency on purpose - I wanted to understand every line.

## Status

Experimental / not finished. It runs, it measurably removes broadband noise on
a synthetic sample, and it sounds... okay-ish. The classic problems are all
here (see Limitations). I would not put this near real audio yet.

## How it works

1. Take the first 0.5s of the clip, assume it is noise-only, average its
   magnitude spectrum -> noise profile.
2. STFT the whole signal (1024-sample Hann frames, 256 hop).
3. Per frame, per bin: `mag' = mag - alpha * noise[bin]`, clamped to a spectral
   floor `beta * mag`. Phase is left untouched.
4. Overlap-add back to the time domain.

That is the Boll 1979 method, basically unchanged.

## Try it

```bash
# generate a noisy test clip (tone stack + white hiss, 0.5s leading silence)
dotnet run --project src/SpectralDenoise -- sample sample.wav

# denoise it
dotnet run --project src/SpectralDenoise -- denoise sample.wav clean.wav
```

The tool prints input vs output RMS so you get a rough before/after number.

## Limitations (the honest part)

- **Musical noise.** The hard subtraction + floor leaves the usual warbly
  artifacts. `beta` masks it a bit but does not fix it.
- **Static noise profile.** Estimated once from the head of the file. If the
  noise drifts (fan spins up, AC kicks in) the whole thing falls apart. Needs a
  running minimum-statistics estimator instead.
- **The 0.5s-silence assumption is fragile.** Real recordings often start
  talking immediately. Then the "noise" profile is actually voice and it eats
  the signal.
- **Mono only.** Stereo is downmixed on load. Fine for voice, wrong for anything
  else.
- No VAD, no proper evaluation (SNR/PESQ), just an eyeball + RMS.

## TODO

- [ ] Minimum-statistics / MMSE noise tracking instead of a single fixed profile
- [ ] Try Wiener filtering as a gentler alternative to hard subtraction
- [ ] Real metric (segmental SNR at least) instead of global RMS
- [ ] Test on an actual noisy voice recording, not just the synthetic tone

## SpectralSubtractor

The core algorithm class that implements classic magnitude spectral subtraction
(Boll 1979). It estimates a noise profile from a quiet region, then subtracts
that profile from every STFT frame while preserving the original phase. The
result is reconstructed via overlap-add.

Public surface:
```csharp
public double Alpha { get; init; }   // over-subtraction factor
public double Beta  { get; init; }   // spectral floor to mask musical noise

public SpectralSubtractor(int frameSize = 1024, int hop = 256)

public double[] EstimateNoiseProfile(ReadOnlySpan<float> noiseOnly)
public float[]   Process(ReadOnlySpan<float> signal, double[] noiseProfile)
```

Minimal usage example:
```csharp
// 1. create instance
var subtractor = new SpectralSubtractor(frameSize: 1024, hop: 256)
{
    Alpha = 2.0,   // aggressive subtraction
    Beta  = 0.02   // 2 % spectral floor
};

// 2. estimate noise profile from leading silence
var noiseProfile = subtractor.EstimateNoiseProfile(noiseOnlySpan);

// 3. denoise the whole signal
float[] cleaned = subtractor.Process(noisySignalSpan, noiseProfile);
```

## SpectralSubtractorExtensions

Extension methods that provide additional functionality for `SpectralSubtractor` including pre-allocated buffers, normalized noise profiles, and silence detection.

## SpectralSubtractorValidation

Provides validation methods for `SpectralSubtractor` instances and noise profiles. The validation checks ensure that the over-subtraction factor (`Alpha`) is at least 1.0 and the spectral floor (`Beta`) is within the valid range [0, 1]. Noise profiles are validated for null values, empty arrays, and negative or infinite values.

Minimal usage examples:

```csharp
// 1. Validate a SpectralSubtractor instance
var subtractor = new SpectralSubtractor(frameSize: 1024, hop: 256)
{
    Alpha = 2.0,
    Beta = 0.02
};

// Check if valid (returns false if invalid)
bool isValid = subtractor.IsValid();

// Get detailed validation errors (returns empty list if valid)
IReadOnlyList<string> errors = subtractor.Validate();

// Throw exception if invalid
subtractor.EnsureValid();

// 2. Validate a noise profile
var noiseProfile = new double[513]; // Typical FFT half-size + 1

// Check if valid (returns false if invalid)
bool profileValid = noiseProfile.IsValidNoiseProfile();

// Get detailed validation errors (returns empty list if valid)
IReadOnlyList<string> profileErrors = noiseProfile.ValidateNoiseProfile();

// Throw exception if invalid
noiseProfile.EnsureValidNoiseProfile();
```

Public surface:
```csharp
public static Span<float> Process(this SpectralSubtractor subtractor, ReadOnlySpan<float> signal, double[] noiseProfile, Span<float> output)
public static double[] EstimateNormalizedNoiseProfile(this SpectralSubtractor subtractor, ReadOnlySpan<float> noiseOnly, bool normalize = true)
public static float[] ProcessWithSilenceDetection(this SpectralSubtractor subtractor, ReadOnlySpan<float> signal, double[] noiseProfile, float silenceThreshold = 0.01f)
public static int GetFrameSize(this SpectralSubtractor subtractor)
public static int GetHopSize(this SpectralSubtractor subtractor)
public static double[] GetWindow(this SpectralSubtractor subtractor)
```

Minimal usage examples:

```csharp
// 1. Process with pre-allocated output buffer (zero-allocation)
var subtractor = new SpectralSubtractor(frameSize: 1024, hop: 256);
var noiseProfile = subtractor.EstimateNormalizedNoiseProfile(noiseOnlySpan);

var outputBuffer = new float[signal.Length];
Span<float> outputSpan = subtractor.Process(signal, noiseProfile, outputBuffer);

// 2. Estimate a normalized noise profile
var normalizedProfile = subtractor.EstimateNormalizedNoiseProfile(noiseOnlySpan);

// 3. Process with automatic silence detection
float[] cleanedWithSilenceDetection = subtractor.ProcessWithSilenceDetection(
    noisySignalSpan, 
    noiseProfile,
    silenceThreshold: 0.005f
);

// 4. Get configuration parameters
int frameSize = subtractor.GetFrameSize(); // 1024
int hopSize = subtractor.GetHopSize();    // 256
double[] window = subtractor.GetWindow();   // Hann window coefficients
```

## Layout

```
SpectralDenoise.sln
src/SpectralDenoise/
  Program.cs             CLI (sample / denoise)
  SpectralSubtractor.cs  the actual algorithm
  Fft.cs                 radix-2 Cooley-Tukey
  WindowFunctions.cs     Hann window
  WavFile.cs             NAudio read/write helpers
```
