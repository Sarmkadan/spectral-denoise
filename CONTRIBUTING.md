# Contributing

Thanks for helping with SpectralDenoise. This guide covers what you need to build, test, and submit a change.

## Prerequisites

- **.NET SDK 10.0.x** (targets `net10.0`; CI uses `10.0.x`). Check with `dotnet --version`.
- No other tools are required. NAudio is restored from NuGet on first build.

## Build

From the repository root:

```bash
dotnet restore SpectralDenoise.slnx
dotnet build SpectralDenoise.slnx -c Release
```

CI builds in `Release`, so check that configuration before opening a PR.

## Run tests

```bash
dotnet test SpectralDenoise.slnx -c Release
```

This runs the unit tests (`tests/SpectralDenoise.Tests`) and the integration tests (`tests/SpectralDenoise.IntegrationTests`). To run a subset:

```bash
dotnet test tests/SpectralDenoise.Tests --filter "FullyQualifiedName~NoiseGate"
```

Tests use synthetic in-memory signals and temporary files. Do not commit WAV fixtures.

## Code conventions

- One public type per file, in the `SpectralDenoise` namespace, using file-scoped `namespace SpectralDenoise;`.
- Public classes are `public sealed`, and behavior is exposed through an `I<Name>` interface.
- Add XML doc comments to all public members.
- Test names follow `Method_Scenario_Expected`.
- Match the existing style of the file you edit. There is no `.editorconfig`; `dotnet format SpectralDenoise.slnx` is available if you want it.

## Branch naming

Branch from `main` and use a type prefix that matches the commit type:

- `feat/<short-description>`: new behavior
- `fix/<short-description>`: bug fixes
- `refactor/<short-description>`: no behavior change
- `docs/<short-description>`: documentation only
- `test/<short-description>`: tests only

Use lowercase words separated by hyphens, for example `fix/nan-guard-in-noise-profile`.

## Commit messages

Use [Conventional Commits](https://www.conventionalcommits.org/): `<type>: <imperative summary>`, lowercase type, no trailing period, about 72 characters or fewer.

```
fix: reject NaN alpha in SpectralSubtractor
docs: document WAV channel limits in README
```

Allowed types: `feat`, `fix`, `refactor`, `docs`, `test`, `chore`. Keep one logical change per commit.

## Pull request checklist

Before requesting review, confirm all of the following:

- [ ] `dotnet build SpectralDenoise.slnx -c Release` succeeds.
- [ ] `dotnet test SpectralDenoise.slnx -c Release` passes with no failures.
- [ ] The change introduces no new compiler or analyzer warnings. The build currently reports existing warnings, so compare against `main` and don't add to them.
- [ ] New or changed public behavior has tests, and public members have XML doc comments.
- [ ] The PR description states what changed and why, and notes any behavior change or new CLI option. If you change a documented algorithm or option, update `README.md` or the relevant file in `docs/`.
- [ ] The PR does not add generated files, WAV outputs, or scratch files to the repository.

Do not edit the stray files at the repository root or under `src/` that are outside the projects in `SpectralDenoise.slnx` (for example `test_*.cs` and the `WavFile.WriteMono(...)` files). They are not part of the build.
