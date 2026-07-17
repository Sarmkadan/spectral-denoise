# SpectralSubtractorExtensions

`SpectralSubtractorExtensions` provides a set of convenience extension methods for the `SpectralSubtractor` class.  
These helpers encapsulate the typical workflow of spectral subtraction: estimating a noise profile, applying the subtraction to a signal, optionally detecting silence, and exposing the internal analysis parameters (frame size, hop size, window function). All methods are static and operate on an existing `SpectralSubtractor` instance.

## API

### `public static Span<float> Process(this SpectralSubtractor subtractor, ReadOnlySpan<float> signal, double[] noiseProfile)`

**Purpose**  
Applies spectral subtraction to the supplied `signal` using a pre‑computed `noiseProfile`. The method returns a mutable `Span<float>` that references the processed samples.

**Parameters**  
* `subtractor` – The `SpectralSubtractor` instance whose configuration (frame size, hop size, window) will be used.  
* `signal` – The input audio samples (mono) to be denoised. Must contain at least one frame of data.  
* `noiseProfile` – Normalized noise magnitude spectrum as returned by `EstimateNormalizedNoiseProfile`. Length must match the FFT size (`subtractor.GetFrameSize() / 2 + 1`).

**Return Value**  
A `Span<float>` that points to the denoised output buffer. The span is valid only while the underlying buffer is not overwritten.

**Exceptions**  
* `ArgumentNullException` – If `subtractor`, `signal`, or `noiseProfile` is `null`.  
* `ArgumentException` – If `noiseProfile.Length` does not match the expected FFT bin count.  
* `InvalidOperationException` – If the `SpectralSubtractor` instance has not been properly initialized.

---

### `public static double[] EstimateNormalizedNoiseProfile(this SpectralSubtractor subtractor, ReadOnlySpan<float> noiseOnly)`

**Purpose**  
Computes a normalized noise magnitude spectrum from a segment that contains only background noise. The result can be supplied to `Process` or `ProcessWithSilenceDetection`.

**Parameters**  
* `subtractor` – The `SpectralSubtractor` instance whose analysis parameters are used.  
* `noiseOnly` – A span of samples that should contain only noise (no speech or desired signal). Must be at least one frame long.

**Return Value**  
A `double[]` containing the normalized magnitude for each FFT bin (`frameSize / 2 + 1` elements).

**Exceptions**  
* `ArgumentNullException` – If `subtractor` or `noiseOnly` is `null`.  
* `ArgumentException` – If `noiseOnly` is shorter than the required frame size.  
* `InvalidOperationException` – If the internal window function cannot be generated.

---

### `public static float[] ProcessWithSilenceDetection(this SpectralSubtractor subtractor, ReadOnlySpan<float> signal, double[] noiseProfile, float silenceThreshold = 0.001f)`

**Purpose**  
Performs spectral subtraction while automatically bypassing frames that are classified as silence. This reduces processing overhead and avoids introducing artifacts in silent passages.

**Parameters**  
* `subtractor` – The configured `SpectralSubtractor`.  
* `signal` – Input audio samples to be processed.  
* `noiseProfile` – Normalized noise spectrum (as produced by `EstimateNormalizedNoiseProfile`).  
* `silenceThreshold` – Energy threshold below which a frame is considered silent. Default is `0.001f`.

**Return Value**  
A new `float[]` containing the denoised audio. The array length matches the length of `signal`.

**Exceptions**  
* `ArgumentNullException` – If any argument is `null`.  
* `ArgumentException` – If `noiseProfile` length is invalid or `silenceThreshold` is negative.  
* `InvalidOperationException` – If the subtractor is not ready for processing.

---

### `public static int GetFrameSize(this SpectralSubtractor subtractor)`

**Purpose**  
Retrieves the FFT frame size (number of samples per analysis window) configured for the `SpectralSubtractor`.

**Parameters**  
* `subtractor` – The instance whose frame size is queried.

**Return Value**  
An `int` representing the frame size (must be a power of two).

**Exceptions**  
* `ArgumentNullException` – If `subtractor` is `null`.

---

### `public static int GetHopSize(this SpectralSubtractor subtractor)`

**Purpose**  
Returns the hop size (step between consecutive frames) used by the subtractor.

**Parameters**  
* `subtractor` – The instance whose hop size is requested.

**Return Value**  
An `int` indicating the number of samples between the start of adjacent frames.

**Exceptions**  
* `ArgumentNullException` – If `subtractor` is `null`.

---

### `public static double[] GetWindow(this SpectralSubtractor subtractor)`

**Purpose**  
Provides the window function (e.g., Hann) applied to each frame before the FFT.

**Parameters**  
* `subtractor` – The instance whose window is retrieved.

**Return Value**  
A `double[]` containing the window coefficients; length equals `GetFrameSize()`.

**Exceptions**  
* `ArgumentNullException` – If `subtractor` is `null`.  
* `InvalidOperationException` – If the window has not been generated.

## Usage

### Example 1 – Basic denoising

