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

## Supported Configurations

The pipeline has specific requirements for proper operation. Below is the compatibility matrix:

| Parameter | Supported Values | Default | Notes |
|-----------|------------------|---------|-------|
| **Sample Rate** | 8 kHz, 16 kHz, 44.1 kHz, 48 kHz, 96 kHz, 192 kHz | 44.1 kHz | Validated by WAV header parsing. Higher rates improve high-frequency resolution. |
| **Bit Depth** | 8-bit PCM, 16-bit PCM, 24-bit PCM, 32-bit PCM, IEEE Float | IEEE Float | Handled by NAudio library. IEEE Float provides best dynamic range. |
| **Channels** | 1 (mono), 2 (stereo) | mono | Stereo files are downmixed to mono. Multi-channel (>2) not supported. |
| **Frame Size** | Power of two: 128, 256, 512, 1024, 2048, 4096, 8192 | 1024 | Must be power of two. Larger sizes improve frequency resolution but increase latency. |
| **Hop Size** | Must satisfy COLA with window: 32, 64, 128, 256, 512, 1024, 2048 | 256 | Hop size must satisfy Constant Overlap-Add (COLA) condition. Common ratios: hop = frameSize/4 or frameSize/2. |
| **Window** | Hann periodic | Hann periodic | Ensures perfect reconstruction when COLA is satisfied. |
| **Noise Estimation Duration** | 0.001 s - 10 s | 0.5 s | Minimum 1 frame of audio required. Longer durations provide more stable estimates but may capture non-stationary noise. |
| **Minimum Clip Length** | > frameSize samples | N/A | Need at least one frame for processing. For 1024 frame size: minimum 1024 samples (23.2 ms at 44.1 kHz). |
| **Channel Layout** | Mono, Stereo (interleaved) | mono | Multi-channel (>2) not supported. Stereo files are downmixed. |

### Configuration Validation

The `SpectralSubtractor` class validates configurations at construction time:

- **Frame size**: Must be a power of two between 128 and 8192 samples
- **Hop size**: Must satisfy the Constant Overlap-Add (COLA) condition with the window function for perfect reconstruction
- **Alpha**: Must be ≥ 1.0 (over-subtraction factor)
- **Beta**: Must be in range [0, 1] (spectral floor to mask musical noise)
- **Sample rate**: Validated by the WAV file reader (8 kHz - 192 kHz)
- **Signal length**: Must be sufficient for processing (≥ frameSize samples)

The `SpectralSubtractorValidation` static class provides comprehensive validation methods:

```csharp
// Validate individual parameters
SpectralSubtractorValidation.EnsureValidFrameSize(frameSize);
SpectralSubtractorValidation.EnsureValidHopSize(hop, window);
SpectralSubtractorValidation.EnsureValidSampleRate(sampleRate);
SpectralSubtractorValidation.EnsureValidSignalLength(signalLength, frameSize);
SpectralSubtractorValidation.EnsureValidAlpha(alpha);
SpectralSubtractorValidation.EnsureValidBeta(beta);
SpectralSubtractorValidation.EnsureValidNoiseEstimationDuration(durationSeconds, sampleRate);

// Validate noise profiles
noiseProfile.EnsureValidNoiseProfile();

// Validate SpectralSubtractor instances
subtractor.EnsureValid();
```

Each validation method provides detailed error messages listing all problems found.




### Example Compatible Configurations

```csharp
// Standard voice processing (44.1 kHz, 1024 frame, 256 hop, 0.5s noise estimation)
var subtractor1 = new SpectralSubtractor(frameSize: 1024, hop: 256)
{
    Alpha = 2.0,
    Beta = 0.02
};

// High-resolution processing (48 kHz, 2048 frame, 512 hop, 1.0s noise estimation)
var subtractor2 = new SpectralSubtractor(frameSize: 2048, hop: 512)
{
    Alpha = 3.0,
    Beta = 0.01
};

// Low-latency processing (16 kHz, 512 frame, 128 hop, 0.25s noise estimation)
var subtractor3 = new SpectralSubtractor(frameSize: 512, hop: 128)
{
    Alpha = 1.5,
    Beta = 0.05
};

// Telephony processing (8 kHz, 256 frame, 64 hop, 0.2s noise estimation)
var subtractor4 = new SpectralSubtractor(frameSize: 256, hop: 64)
{
    Alpha = 2.5,
    Beta = 0.03
};
```

### Incompatible Configurations

```csharp
// These will throw ArgumentException:
var bad1 = new SpectralSubtractor(frameSize: 1000); // Not power of two
var bad2 = new SpectralSubtractor(frameSize: 1024, hop: 200); // Doesn't satisfy COLA
var bad3 = new SpectralSubtractor(frameSize: 1024) { Alpha = 0.5 }; // Alpha < 1.0
var bad4 = new SpectralSubtractor(frameSize: 1024) { Beta = 1.5 }; // Beta > 1.0
```

### Incompatible Configurations

```csharp
// These will throw ArgumentException:
var bad1 = new SpectralSubtractor(frameSize: 1000); // Not power of two
var bad2 = new SpectralSubtractor(frameSize: 1024, hop: 200); // Doesn't satisfy COLA
```

## Recommended Settings

### Voice Recordings (44.1 kHz)
- **Frame size**: 1024 samples (23.2 ms)
- **Hop size**: 256 samples (5.8 ms)
- **Window**: Hann periodic (default)
- **Noise estimation**: 0.5-1.0 seconds of leading silence
- **Alpha** (over-subtraction): 2.0-4.0
- **Beta** (spectral floor): 0.01-0.05

### Telephony (8 kHz)
- **Frame size**: 256 samples (32 ms)
- **Hop size**: 64 samples (8 ms)
- **Window**: Hann periodic
- **Noise estimation**: 0.2-0.5 seconds
- **Alpha**: 2.5-5.0 (more aggressive due to limited frequency resolution)
- **Beta**: 0.02-0.08

### High-Resolution Audio (96 kHz / 192 kHz)
- **Frame size**: 2048-4096 samples (21.3-42.7 ms at 96 kHz)
- **Hop size**: 512-1024 samples (5.3-10.7 ms at 96 kHz)
- **Window**: Hann periodic
- **Noise estimation**: 1.0-2.0 seconds
- **Alpha**: 1.5-3.0 (less aggressive due to better frequency resolution)
- **Beta**: 0.005-0.02

### General Guidelines
- **Frame size**: Choose based on desired time-frequency trade-off. Larger frames = better frequency resolution but worse time resolution.
- **Hop size**: Typically frameSize/4 or frameSize/2 for COLA compliance with Hann windows.
- **Alpha**: Start with 2.0 and adjust based on musical noise artifacts. Higher values remove more noise but increase artifacts.
- **Beta**: Start with 0.02 and increase if you hear musical noise. Lower values preserve more signal but may leave residual noise.
- **Noise estimation**: Use longer durations for stationary noise, shorter for non-stationary noise. The 0.5s default works well for most voice recordings with leading silence.

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
