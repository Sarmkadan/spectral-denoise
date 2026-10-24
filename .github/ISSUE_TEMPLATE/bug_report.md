---
name: Bug report
about: Report a crash, wrong output, or unexpected behaviour in the denoiser or CLI
title: "[Bug]: "
labels: ["bug", "triage"]
assignees: []
---

## Summary

<!-- One or two sentences describing the problem. -->

## Steps to reproduce

1. <!-- e.g. dotnet run --project src/SpectralDenoise -- denoise in.wav out.wav --alpha 2.5 -->
2.
3.

Minimal command line used:

```bash

```

## Expected behaviour

<!-- What you expected to happen. -->

## Actual behaviour

<!-- What happened instead. Include the full console output, including any "Error: ..." message and exit code (0-6). -->

```text

```

## Sample audio and parameters

<!-- Attach a short WAV clip that reproduces the issue if you can (a few seconds is enough). Do not attach private recordings. -->

- Input file (attached or synthetic description):
- Sample rate / bit depth / channels:
- Mode (`subtract` or `wiener`):
- `--alpha`:
- `--floor`:
- `--noise-seconds`:

## Environment

- .NET SDK version (`dotnet --version`):
- Target framework / package versions (NAudio, test SDK if relevant):
- OS and version (e.g. Ubuntu 24.04, Windows 11 23H2, macOS 15.1):
- CPU architecture (x64 / arm64):
- Project version or commit SHA:

## Additional context

<!-- Anything else: logs, screenshots, suspected cause, whether it is a regression. -->
