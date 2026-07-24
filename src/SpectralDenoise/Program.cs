using SpectralDenoise;

// denoise <input.wav> <output.wav> [--alpha VALUE] [--floor VALUE] [--noise-seconds VALUE]

if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
{
    PrintUsage();
    return 0;
}

string? inputPath = null;
string? outputPath = null;
double noiseSeconds = 0.5;
double alpha = 2.0;
double floor = 0.02;

for (int i = 0; i < args.Length; i++)
{
    string arg = args[i];

    if (arg == "--help" || arg == "-h")
    {
        PrintUsage();
        return 0;
    }
    else if (arg.StartsWith("--"))
    {
        if (i + 1 >= args.Length)
        {
            Console.Error.WriteLine($"Error: Missing value for argument {arg}");
            PrintUsage();
            return 2; // Exit code 2 for missing argument value
        }

        string value = args[++i];
        switch (arg)
        {
            case "--alpha":
                if (!double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out alpha) || alpha <= 0)
                {
                    Console.Error.WriteLine("Error: --alpha must be a positive number.");
                    PrintUsage();
                    return 2; // Exit code 2 for invalid alpha value
                }
                break;

            case "--floor":
                if (!double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out floor) || floor < 0)
                {
                    Console.Error.WriteLine("Error: --floor must be a non-negative number.");
                    PrintUsage();
                    return 2; // Exit code 2 for invalid floor value
                }
                break;

            case "--noise-seconds":
                if (!double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out noiseSeconds) || noiseSeconds <= 0)
                {
                    Console.Error.WriteLine("Error: --noise-seconds must be a positive number.");
                    PrintUsage();
                    return 2; // Exit code 2 for invalid noise-seconds value
                }
                break;

            default:
                Console.Error.WriteLine($"Error: Unknown argument {arg}");
                PrintUsage();
                return 2; // Exit code 2 for unknown argument
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
            return 2; // Exit code 2 for too many arguments
        }
    }
}

// Validate required arguments
if (inputPath == null)
{
    Console.Error.WriteLine("Error: Input file path is required.");
    PrintUsage();
    return 2; // Exit code 2 for missing input file
}

if (outputPath == null)
{
    Console.Error.WriteLine("Error: Output file path is required.");
    PrintUsage();
    return 2; // Exit code 2 for missing output file
}

// Refuse to overwrite input file
if (string.Equals(inputPath, outputPath, StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("Error: Output file cannot be the same as input file.");
    PrintUsage();
    return 3; // Exit code 3 for attempting to overwrite input
}

// Validate file existence
if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"Error: Input file not found: {inputPath}");
    PrintUsage();
    return 4; // Exit code 4 for missing input file
}

// Validate output directory exists
string? outputDirectory = Path.GetDirectoryName(outputPath);
if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
{
    Console.Error.WriteLine($"Error: Output directory does not exist: {outputDirectory}");
    PrintUsage();
    return 5; // Exit code 5 for invalid output directory
}

// Validate output file won't overwrite existing file without permission
if (File.Exists(outputPath))
{
    Console.Error.WriteLine($"Error: Output file already exists: {outputPath}");
    Console.Error.WriteLine("Use --force to overwrite (not implemented in this version).");
    PrintUsage();
    return 6; // Exit code 6 for existing output file
}

return Denoise(inputPath, outputPath, noiseSeconds, alpha, floor);

static void PrintUsage()
{
    Console.Error.WriteLine("usage:");
    Console.Error.WriteLine(" denoise <input.wav> <output.wav> [--alpha VALUE] [--floor VALUE] [--noise-seconds VALUE]");
    Console.Error.WriteLine();
    Console.Error.WriteLine("positional arguments:");
    Console.Error.WriteLine(" input.wav   Input WAV file path (required)");
    Console.Error.WriteLine(" output.wav  Output WAV file path (required)");
    Console.Error.WriteLine();
    Console.Error.WriteLine("options:");
    Console.Error.WriteLine(" --help, -h         Show this help message");
    Console.Error.WriteLine(" --alpha VALUE        Spectral subtraction alpha parameter (default: 2.0)");
    Console.Error.WriteLine(" --floor VALUE        Spectral subtraction floor parameter (default: 0.02)");
    Console.Error.WriteLine(" --noise-seconds VALUE Seconds of audio to sample for noise estimation (default: 0.5)");
    Console.Error.WriteLine();
    Console.Error.WriteLine("examples:");
    Console.Error.WriteLine(" denoise input.wav output.wav");
    Console.Error.WriteLine(" denoise input.wav output.wav --noise-seconds 1.0");
    Console.Error.WriteLine(" denoise input.wav output.wav --alpha 2.5 --floor 0.01");
    Console.Error.WriteLine(" denoise --help");
}

static int Denoise(string inPath, string outPath, double noiseSeconds, double alpha, double floor)
{
    SpectralSubtractor sub;
    double[] profile;
    int sr;

    // Try to read as stereo first
    try
    {
        var (left, right, sampleRate) = WavFile.ReadStereo(inPath);
        sr = sampleRate;
        Console.WriteLine($"loaded stereo: {left.Length} samples @ {sr}Hz (left + right)");

        sub = new SpectralSubtractor(frameSize: 1024, hop: 256)
        {
            Alpha = alpha,
            Beta = floor,
        };

        // Estimate noise profile from the first channel (assuming both channels have similar noise)
        int noiseLen = Math.Min(left.Length, (int)(sr * noiseSeconds));
        if (noiseLen == 0)
        {
            Console.Error.WriteLine("Error: Not enough samples to estimate noise profile.");
            return 1; // Exit code 1 for processing failure
        }

        profile = sub.EstimateNoiseProfile(left.AsSpan(0, noiseLen));
        Console.WriteLine($"estimated noise profile from {noiseLen} samples ({noiseSeconds}s)");

        // Process each channel independently with progress reporting
        var leftProgress = new Progress<double>(p => Console.WriteLine($"Left channel progress: {p:P0}"));
        Console.WriteLine("denoising left channel...");
        var cleanedLeft = sub.Process(left, profile, leftProgress);

        var rightProgress = new Progress<double>(p => Console.WriteLine($"Right channel progress: {p:P0}"));
        Console.WriteLine("denoising right channel...");
        var cleanedRight = sub.Process(right, profile, rightProgress);

        WavFile.WriteStereo(outPath, cleanedLeft, cleanedRight, sr);

        Console.WriteLine($"wrote {outPath}");
        Console.WriteLine($"input RMS left: {Rms(left):F5}, right: {Rms(right):F5}");
        Console.WriteLine($"output RMS left: {Rms(cleanedLeft):F5}, right: {Rms(cleanedRight):F5}");
        return 0;
    }
    catch (InvalidDataException)
    {
        // Fall back to mono processing if not stereo
        var (samples, sampleRate) = WavFile.ReadMono(inPath);
        sr = sampleRate;
        Console.WriteLine($"loaded {samples.Length} samples @ {sr}Hz");

        sub = new SpectralSubtractor(frameSize: 1024, hop: 256)
        {
            Alpha = alpha,
            Beta = floor,
        };

        int noiseLen = Math.Min(samples.Length, (int)(sr * noiseSeconds));
        if (noiseLen == 0)
        {
            Console.Error.WriteLine("Error: Not enough samples to estimate noise profile.");
            return 1; // Exit code 1 for processing failure
        }

        profile = sub.EstimateNoiseProfile(samples.AsSpan(0, noiseLen));
        Console.WriteLine($"estimated noise profile from {noiseLen} samples ({noiseSeconds}s)");

        var monoProgress = new Progress<double>(p => Console.WriteLine($"Progress: {p:P0}"));
        var cleaned = sub.Process(samples, profile, monoProgress);
        WavFile.WriteMono(outPath, cleaned, sr);

        Console.WriteLine($"wrote {outPath}");
        Console.WriteLine($"input RMS {Rms(samples):F5}");
        Console.WriteLine($"output RMS {Rms(cleaned):F5}");
        return 0;
    }
    catch (Exception ex) when (ex is not InvalidDataException)
    {
        Console.Error.WriteLine($"Error: Failed to process audio: {ex.Message}");
        return 1; // Exit code 1 for processing failure
    }
}

static double Rms(float[] x)
{
    double acc = 0;
    foreach (var v in x) acc += (double)v * v;
    return Math.Sqrt(acc / x.Length);
}
