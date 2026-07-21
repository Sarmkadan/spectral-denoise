# NoiseProfile

A `NoiseProfile` holds a spectral noise estimate used by spectral noise reduction algorithms such as SpectralSubtractor. It contains a magnitude spectrum, the sample rate, frame size, and hop used to derive the estimate, and provides serialization and validation utilities.

## API

### `public double[] Magnitudes`
Gets the magnitude spectrum of the noise estimate, in units of power spectral density. The array length equals `FrameSize / 2 + 1` and corresponds to non-redundant frequency bins from DC to Nyquist.

### `public int SampleRate`
Gets the sample rate (Hz) at which the noise profile was estimated. Used to map frequency bins to physical frequencies.

### `public int FrameSize`
Gets the FFT frame size (samples) used when estimating the noise profile. Determines the frequency resolution of `Magnitudes`.

### `public int Hop`
Gets the hop size (samples) used when estimating the noise profile. Affects temporal resolution and overlap between successive frames.

### `public NoiseProfile()`
Constructs a default-initialized profile with empty `Magnitudes`, `SampleRate` = 0, `FrameSize` = 0, and `Hop` = 0. Callers must populate fields or use a factory method before use.

### `public static NoiseProfile FromEstimate(double[] magnitudes, int sampleRate, int frameSize, int hop)`
Creates a `NoiseProfile` from an estimated magnitude spectrum and processing parameters.
- `magnitudes`: non-null array of power spectral density values.
- `sampleRate`: sample rate in Hz; must be > 0.
- `frameSize`: FFT frame size in samples; must be a positive power of two.
- `hop`: hop size in samples; must be positive and ≤ `frameSize`.
Throws `ArgumentNullException` if `magnitudes` is null.
Throws `ArgumentOutOfRangeException` if any parameter is invalid.

### `public string ToJson()`
Serializes the profile to a compact JSON string containing `SampleRate`, `FrameSize`, `Hop`, and `Magnitudes`.
Returns an empty string if the profile is invalid (e.g., `SampleRate` ≤ 0).

### `public static NoiseProfile FromJson(string json)`
Deserializes a JSON string produced by `ToJson` into a `NoiseProfile`.
- `json`: non-null JSON string.
Throws `ArgumentNullException` if `json` is null.
Throws `JsonException` if the JSON is malformed or required fields are missing.

### `public static bool TryFromJson(string json, out NoiseProfile profile)`
Attempts to deserialize a JSON string into a `NoiseProfile`.
- `json`: non-null JSON string.
- `profile`: receives the deserialized profile on success.
Returns `true` on success; otherwise `false` and sets `profile` to `null`.
Throws `ArgumentNullException` if `json` is null.

### `public void Validate()`
Validates the profile’s invariants.
Throws `InvalidOperationException` if `SampleRate` ≤ 0, `FrameSize` ≤ 0, `Hop` ≤ 0, or `Magnitudes` is null or its length does not match `FrameSize / 2 + 1`.

## Usage
