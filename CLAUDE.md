# CLAUDE.md

Experimental C#/.NET 10 CLI that removes broadband noise from WAV files via spectral subtraction (Boll 1979), using NAudio for I/O and a hand-rolled radix-2 FFT.

## Build

```bash
dotnet build SpectralDenoise.slnx
dotnet run --project src/SpectralDenoise -- sample sample.wav            # generate noisy test clip
dotnet run --project src/SpectralDenoise -- denoise in.wav out.wav       # denoise (see --help for --alpha/--floor/--noise-seconds/--mode)
```

## Test

```bash
dotnet test SpectralDenoise.slnx
dotnet test tests/SpectralDenoise.Tests --filter "FullyQualifiedName~NoiseGate"
```

xUnit 2.9, `Microsoft.NET.Test.Sdk`. Test project references `src/SpectralDenoise` directly.

## Lint / format

No analyzers or `.editorconfig` configured. Use `dotnet format SpectralDenoise.slnx` if needed. `Nullable` and `ImplicitUsings` are enabled in both projects.

## Layout

- `SpectralDenoise.slnx` - solution (XML slnx format); only the two projects below are included.
- `src/SpectralDenoise/` - library + CLI (`OutputType=Exe`).
  - `Program.cs` - top-level-statements CLI entry point; arg parsing, exit codes 0-6.
  - `AudioPipeline.cs` (`IAudioProcessor`) - orchestrates read -> profile -> subtract -> write.
  - `SpectralSubtractor.cs` (`ISpectralSubtractor`), `NoiseProfile.cs`, `NoiseGate.cs` - DSP core.
  - `Fft.cs`, `WindowFunctions.cs`, `SampleBufferExtensions.cs` - primitives.
  - `WavFile.cs`, `IAudioFileReader.cs`, `IAudioFileWriter.cs` - NAudio-based I/O.
- `tests/SpectralDenoise.Tests/` - xUnit tests, one `<Type>Tests.cs` per type.
- `docs/` - per-type markdown docs (`NoiseProfile.md`, `SpectralSubtractor.md`, ...).
- `README.md` - algorithm description, limitations, supported WAV formats. `STREAMING_IMPLEMENTATION.md` - streaming design notes.
- Stray files not in any project (ignore, do not extend): `src/*.cs`, `src/SpectralDenoise.Tests/`, `test_*.cs*`, and two root files named `WavFile.WriteMono(...)`.

## Conventions

- Single namespace `SpectralDenoise`, file-scoped `namespace SpectralDenoise;`, one public type per file.
- Classes are `public sealed`; behavior is exposed via interfaces (`I<Name>`) for testability.
- Per-type companion files by suffix: `<Type>Constants.cs` (`internal static class`, `const` defaults/limits), `<Type>Validation.cs` (argument checks), `<Type>Extensions.cs`, `<Type>JsonExtensions.cs` (System.Text.Json, camelCase, cached `JsonSerializerOptions`).
- Private static fields `_camelCase`; constants `PascalCase`; XML doc comments on public members.
- Audio is `float[]` mono; sample rate passed explicitly as `int`. STFT defaults: 1024 Hann frame, 256 hop, 0.5 s noise head, alpha 2.0, floor 0.02.
- Tests are named `Method_Scenario_Expected`, use `[Fact]`/`[Theory]`, synthetic in-memory signals; WAV tests write to temp files (no committed fixtures).
