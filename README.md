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
