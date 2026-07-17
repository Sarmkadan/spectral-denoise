# SpectralSubtractorValidation

Utility class providing validation methods for spectral subtraction operations and noise profile data used by `SpectralSubtractor`. These methods ensure input parameters and noise profiles meet required constraints before processing audio data, preventing runtime errors and undefined behavior during spectral subtraction.

## API

### `public static IReadOnlyList<string> Validate`

Validates the core parameters required for spectral subtraction. Returns a list of validation error messages; an empty list indicates all parameters are valid.

- **Parameters**: None
- **Return value**: `IReadOnlyList<string>` – Collection of error messages. Empty if valid.
- **Exceptions**: None

### `public static bool IsValid`

Determines whether the core parameters required for spectral subtraction are valid. Returns `true` if all parameters meet constraints.

- **Parameters**: None
- **Return value**: `bool` – `true` if parameters are valid; otherwise `false`.
- **Exceptions**: None

### `public static void EnsureValid`

Validates the core parameters required for spectral subtraction and throws an exception if any parameter is invalid.

- **Parameters**: None
- **Return value**: `void`
- **Exceptions**: Throws `InvalidOperationException` with a descriptive message if validation fails.

### `public static IReadOnlyList<string> ValidateNoiseProfile`

Validates a noise profile used in spectral subtraction. Returns a list of validation error messages; an empty list indicates the profile is valid.

- **Parameters**: None
- **Return value**: `IReadOnlyList<string>` – Collection of error messages. Empty if valid.
- **Exceptions**: None

### `public static bool IsValidNoiseProfile`

Determines whether a noise profile used in spectral subtraction is valid. Returns `true` if the profile meets constraints.

- **Parameters**: None
- **Return value**: `bool` – `true` if the noise profile is valid; otherwise `false`.
- **Exceptions**: None

### `public static void EnsureValidNoiseProfile`

Validates a noise profile used in spectral subtraction and throws an exception if the profile is invalid.

- **Parameters**: None
- **Return value**: `void`
- **Exceptions**: Throws `InvalidOperationException` with a descriptive message if validation fails.

## Usage
