# SpectralSubtractor

A spectral-domain noise reduction processor that attenuates noise in audio frames using spectral subtraction techniques. It estimates a noise profile from input frames and applies frequency-dependent attenuation based on the `DenoiseMode` configuration. The class supports dynamic adaptation via smoothing parameters and provides control over over-subtraction and spectral floor constraints.

## API

### `public double Alpha`
Gets or sets the smoothing coefficient for noise profile estimation (range `[0, 1]`). Higher values increase responsiveness to changes in noise characteristics but may introduce instability. Default is `0.9`.

### `public double Beta`
Gets or sets the smoothing coefficient for spectral gain application (range `[0, 1]`). Controls how aggressively the denoiser adapts to transients. Default is `0.7`.

### `public double OverSubtractionFactor`
Gets or sets the over-subtraction factor applied during spectral subtraction (range `[0, 1]`). Higher values suppress more noise but risk introducing artifacts. Default is `1.0`.

### `public double SpectralFloor`
Gets or sets the minimum magnitude threshold for spectral components (in dB). Components below this level are floored to prevent excessive attenuation. Default is `-80.0`.

### `public DenoiseMode Mode`
Gets or sets the denoising mode, selecting between aggressive, moderate, or conservative spectral subtraction strategies. Default is `DenoiseMode.Moderate`.

### `public double AttackMs`
Gets or sets the attack time (in milliseconds) for gain smoothing, controlling how quickly the denoiser reacts to increases in noise. Default is `20.0`.

### `public double ReleaseMs`
Gets or sets the release time (in milliseconds) for gain smoothing, controlling how quickly the denoiser recovers after noise decreases. Default is `100.0`.

### `public SpectralSubtractor()`
Constructs a new `SpectralSubtractor` with default parameters.

### `public void ResetSmoothing()`
Resets the internal smoothing state, clearing any accumulated history. Call this when switching audio streams or after long periods of silence to prevent artifacts.

### `public double[] EstimateNoiseProfile(float[] input)`
Estimates a noise profile from the provided input frame.

- **Parameters**:
  - `input`: A single-channel audio frame (non-interleaved) of arbitrary length.
- **Returns**: A power spectrum array representing the estimated noise profile.
- **Throws**: `ArgumentNullException` if `input` is `null`.
- **Throws**: `ArgumentException` if `input` is empty.

### `public float[] Process(float[] input)`
Applies spectral denoising to the input frame.

- **Parameters**:
  - `input`: A single-channel audio frame (non-interleaved) of arbitrary length.
- **Returns**: A denoised audio frame of the same length as `input`.
- **Throws**: `ArgumentNullException` if `input` is `null`.
- **Throws**: `ArgumentException` if `input` is empty.

## Usage

### Basic Noise Reduction
