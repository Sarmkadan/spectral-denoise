using SpectralDenoise;

// spectral-denoise <input.wav> <output.wav> [--noise-seconds N] [--frame-size N] [--hop N]

if (args.Length < 2)
{
    PrintUsage();
    return 1;
}

string inputPath = null;
string outputPath = null;
double noiseSeconds = 0.5;
int frameSize = 1024;
int hop = 256;

for (int i = 0; i < args.Length; i++)
{
    string arg = args[i];
    if (arg.StartsWith("--"))
    {
        if (i + 1 >= args.Length)
        {
            Console.Error.WriteLine($"Error: Missing value for argument {arg}");
            PrintUsage();
            return 1;
        }

        string value = args[++i];
        switch (arg)
        {
            case "--noise-seconds":
                if (!double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out noiseSeconds) || noiseSeconds <= 0)
                {
                    Console.Error.WriteLine("Error: --noise-seconds must be a positive number.");
                    return 1;
                }
                break;

            case "--frame-size":
                if (!int.TryParse(value, out frameSize) || frameSize <= 0)
                {
                    Console.Error.WriteLine("Error: --frame-size must be a positive integer.");
                    return 1;
                }
                if (!IsPowerOfTwo(frameSize))
                {
                    Console.Error.WriteLine("Error: --frame-size must be a power of two.");
                    return 1;
                }
                break;

            case "--hop":
                if (!int.TryParse(value, out hop) || hop <= 0)
                {
                    Console.Error.WriteLine("Error: --hop must be a positive integer.");
                    return 1;
                }
                break;

            default:
                Console.Error.WriteLine($"Error: Unknown argument {arg}");
                PrintUsage();
                return 1;
        }
    }
    else
    {
        // Positional arguments
        if (inputPath == null)
        {
            inputPath = arg;
        }
        else if (outputPath == null)
        {
            outputPath = arg;
        }
        else
        {
            Console.Error.WriteLine("Error: Too many arguments provided.");
            PrintUsage();
            return 1;
        }
    }
}

if (inputPath == null || outputPath == null)
{
    Console.Error.WriteLine("Error: Input and output paths are required.");
    PrintUsage();
    return 1;
}

Denoise(inputPath, outputPath, noiseSeconds, frameSize, hop);
return 0;

static void PrintUsage()
{
    Console.Error.WriteLine("usage:");
    Console.Error.WriteLine("  spectral-denoise <input.wav> <output.wav> [options]");
    Console.Error.WriteLine();
    Console.Error.WriteLine("options:");
    Console.Error.WriteLine("  --noise-seconds N   Seconds of noise to sample from start of file (default: 0.5)");
    Console.Error.WriteLine("  --frame-size N      FFT frame size, must be power of two (default: 1024)");
    Console.Error.WriteLine("  --hop N             Hop size between frames (default: 256)");
}

static bool IsPowerOfTwo(int n)
{
    return n > 0 && (n & (n - 1)) == 0;
}

static void Denoise(string inPath, string outPath, double noiseSeconds, int frameSize, int hop)
{
    var (samples, sr) = WavFile.ReadMono(inPath);
    Console.WriteLine($"loaded {samples.Length} samples @ {sr}Hz");

    var sub = new SpectralSubtractor(frameSize: frameSize, hop: hop)
    {
        Alpha = 2.0,
        Beta = 0.02,
    };

    int noiseLen = Math.Min(samples.Length, (int)(sr * noiseSeconds));
    if (noiseLen == 0)
    {
        Console.Error.WriteLine("Error: Not enough samples to estimate noise profile.");
        Environment.Exit(1);
    }

    var profile = sub.EstimateNoiseProfile(samples.AsSpan(0, noiseLen));
    Console.WriteLine($"estimated noise profile from {noiseLen} samples ({noiseSeconds}s)");

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
