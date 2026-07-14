using SpectralDenoise;

// Tiny CLI. Two verbs:
//   sample <out.wav>            generate a noisy test clip (voice-ish tone + hiss)
//   denoise <in.wav> <out.wav>  run spectral subtraction
//
// The noise profile is taken from the first 0.5s of the input, which we assume
// is silence-plus-noise. That assumption is the whole ballgame - see README.

if (args.Length == 0)
{
    PrintUsage();
    return 1;
}

switch (args[0])
{
    case "sample":
        if (args.Length < 2) { PrintUsage(); return 1; }
        GenerateSample(args[1]);
        return 0;

    case "denoise":
        if (args.Length < 3) { PrintUsage(); return 1; }
        Denoise(args[1], args[2]);
        return 0;

    default:
        PrintUsage();
        return 1;
}

static void PrintUsage()
{
    Console.Error.WriteLine("usage:");
    Console.Error.WriteLine("  spectral-denoise sample <out.wav>");
    Console.Error.WriteLine("  spectral-denoise denoise <in.wav> <out.wav>");
}

static void GenerateSample(string outPath)
{
    const int sr = 16000;
    const double seconds = 3.0;
    int n = (int)(sr * seconds);
    var buf = new float[n];
    var rng = new Random(1234);

    for (int i = 0; i < n; i++)
    {
        double t = (double)i / sr;
        // 0.5s of leading "silence" (noise only), then a wobbly tone stack
        double voice = 0.0;
        if (t > 0.5)
        {
            voice += 0.30 * Math.Sin(2 * Math.PI * 220 * t);
            voice += 0.15 * Math.Sin(2 * Math.PI * 440 * t);
            voice += 0.08 * Math.Sin(2 * Math.PI * 660 * t);
            voice *= 0.6 + 0.4 * Math.Sin(2 * Math.PI * 3 * t); // amplitude wobble
        }
        double hiss = 0.05 * (rng.NextDouble() * 2 - 1);
        buf[i] = (float)(voice + hiss);
    }

    WavFile.WriteMono(outPath, buf, sr);
    Console.WriteLine($"wrote {outPath} ({seconds}s @ {sr}Hz, first 0.5s is noise-only)");
}

static void Denoise(string inPath, string outPath)
{
    var (samples, sr) = WavFile.ReadMono(inPath);
    Console.WriteLine($"loaded {samples.Length} samples @ {sr}Hz");

    var sub = new SpectralSubtractor(frameSize: 1024, hop: 256)
    {
        Alpha = 2.0,
        Beta = 0.02,
    };

    int noiseLen = Math.Min(samples.Length, sr / 2); // first 0.5s
    var profile = sub.EstimateNoiseProfile(samples.AsSpan(0, noiseLen));
    Console.WriteLine($"estimated noise profile from {noiseLen} samples");

    var cleaned = sub.Process(samples, profile);
    WavFile.WriteMono(outPath, cleaned, sr);

    Console.WriteLine($"wrote {outPath}");
    Console.WriteLine($"input RMS  {Rms(samples):F5}");
    Console.WriteLine($"output RMS {Rms(cleaned):F5}");
}

static double Rms(float[] x)
{
    double acc = 0;
    foreach (var v in x) acc += (double)v * v;
    return Math.Sqrt(acc / x.Length);
}
