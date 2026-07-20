using SpectralDenoise;

// spectral-denoise [--help] [--input PATH] [--output PATH] [--noise-frames N] [--noise-seconds N] [--frame-size N] [--overlap N] [--hop N] [--save-noise PATH] [--load-noise PATH]

if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
{
    PrintUsage();
    return 0;
}

string inputPath = null;
string outputPath = null;
double noiseSeconds = 0.5;
int frameSize = 1024;
int hop = 256;
string? saveNoisePath = null;
string? loadNoisePath = null;

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
            return 2;
        }

        string value = args[++i];
        switch (arg)
        {
            case "--input":
                inputPath = value;
                break;

            case "--output":
                outputPath = value;
                break;

            case "--noise-frames":
                if (!int.TryParse(value, out var noiseFrames) || noiseFrames <= 0)
                {
                    Console.Error.WriteLine("Error: --noise-frames must be a positive integer.");
                    PrintUsage();
                    return 2;
                }
                // Convert frames to seconds based on typical sample rate (44100 Hz)
                noiseSeconds = noiseFrames / 44100.0;
                break;

            case "--noise-seconds":
                if (!double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out noiseSeconds) || noiseSeconds <= 0)
                {
                    Console.Error.WriteLine("Error: --noise-seconds must be a positive number.");
                    PrintUsage();
                    return 2;
                }
                break;

            case "--frame-size":
                if (!int.TryParse(value, out frameSize) || frameSize <= 0)
                {
                    Console.Error.WriteLine("Error: --frame-size must be a positive integer.");
                    PrintUsage();
                    return 2;
                }
                if (!IsPowerOfTwo(frameSize))
                {
                    Console.Error.WriteLine("Error: --frame-size must be a power of two.");
                    PrintUsage();
                    return 2;
                }
                break;

            case "--overlap":
                if (!int.TryParse(value, out hop) || hop <= 0)
                {
                    Console.Error.WriteLine("Error: --overlap must be a positive integer.");
                    PrintUsage();
                    return 2;
                }
                break;

            case "--hop":
                if (!int.TryParse(value, out hop) || hop <= 0)
                {
                    Console.Error.WriteLine("Error: --hop must be a positive integer.");
                    PrintUsage();
                    return 2;
                }
                break;

            case "--save-noise":
                saveNoisePath = value;
                break;

            case "--load-noise":
                loadNoisePath = value;
                break;

            default:
                Console.Error.WriteLine($"Error: Unknown argument {arg}");
                PrintUsage();
                return 2;
        }
    }
    else
    {
        // Positional arguments (for backwards compatibility)
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
            return 2;
        }
    }
}

if (inputPath == null)
{
    Console.Error.WriteLine("Error: --input is required.");
    PrintUsage();
    return 2;
}

if (outputPath == null)
{
    Console.Error.WriteLine("Error: --output is required.");
    PrintUsage();
    return 2;
}

return Denoise(inputPath, outputPath, noiseSeconds, frameSize, hop, saveNoisePath, loadNoisePath);

static void PrintUsage()
{
    Console.Error.WriteLine("usage:");
    Console.Error.WriteLine(" spectral-denoise [--help] [--input PATH] [--output PATH] [options]");
    Console.Error.WriteLine();
    Console.Error.WriteLine("options:");
    Console.Error.WriteLine(" --help, -h              Show this help message");
    Console.Error.WriteLine(" --input PATH             Input WAV file path (required)");
    Console.Error.WriteLine(" --output PATH            Output WAV file path (required)");
    Console.Error.WriteLine(" --noise-frames N        Number of frames to sample for noise estimation");
    Console.Error.WriteLine(" --noise-seconds N       Seconds of noise to sample from start of file (default: 0.5)");
    Console.Error.WriteLine(" --frame-size N          FFT frame size, must be power of two (default: 1024)");
    Console.Error.WriteLine(" --overlap N             Overlap between frames (default: 256)");
    Console.Error.WriteLine(" --hop N                 Hop size between frames (default: 256)");
    Console.Error.WriteLine(" --save-noise PATH        Save noise profile to JSON file");
    Console.Error.WriteLine(" --load-noise PATH        Load noise profile from JSON file");
    Console.Error.WriteLine();
    Console.Error.WriteLine("examples:");
    Console.Error.WriteLine(" spectral-denoise --input input.wav --output output.wav");
    Console.Error.WriteLine(" spectral-denoise --input input.wav --output output.wav --noise-seconds 1.0");
    Console.Error.WriteLine(" spectral-denoise --help");
}

static bool IsPowerOfTwo(int n)
{
    return n > 0 && (n & (n - 1)) == 0;
}

static int Denoise(string inPath, string outPath, double noiseSeconds, int frameSize, int hop, string? saveNoisePath = null, string? loadNoisePath = null)
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

        sub = new SpectralSubtractor(frameSize: frameSize, hop: hop)
        {
            Alpha = 2.0,
            Beta = 0.02,
        };

        // Load noise profile if specified
        if (loadNoisePath != null)
        {
            if (!File.Exists(loadNoisePath))
            {
                Console.Error.WriteLine($"Error: Noise profile file not found: {loadNoisePath}");
                return 1;
            }

            try
            {
                var noiseProfile = NoiseProfile.FromJson(File.ReadAllText(loadNoisePath));
                noiseProfile.Validate(sr, frameSize, hop);
                profile = noiseProfile.Magnitudes;
                Console.WriteLine($"loaded noise profile from {loadNoisePath} ({profile.Length} bins, {sr}Hz, frameSize={frameSize}, hop={hop})");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: Failed to load noise profile: {ex.Message}");
                return 1;
            }
        }
        else
        {
            // Estimate noise profile from the first channel (assuming both channels have similar noise)
            int noiseLen = Math.Min(left.Length, (int)(sr * noiseSeconds));
            if (noiseLen == 0)
            {
                Console.Error.WriteLine("Error: Not enough samples to estimate noise profile.");
                return 1;
            }

            profile = sub.EstimateNoiseProfile(left.AsSpan(0, noiseLen));
            Console.WriteLine($"estimated noise profile from {noiseLen} samples ({noiseSeconds}s)");

            // Save noise profile if specified
            if (saveNoisePath != null)
            {
                var noiseProfile = NoiseProfile.FromEstimate(profile, sr, sub);
                File.WriteAllText(saveNoisePath, noiseProfile.ToJson(true));
                Console.WriteLine($"saved noise profile to {saveNoisePath}");
            }
        }

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

        sub = new SpectralSubtractor(frameSize: frameSize, hop: hop)
        {
            Alpha = 2.0,
            Beta = 0.02,
        };

        // Load noise profile if specified
        if (loadNoisePath != null)
        {
            if (!File.Exists(loadNoisePath))
            {
                Console.Error.WriteLine($"Error: Noise profile file not found: {loadNoisePath}");
                return 1;
            }

            try
            {
                var noiseProfile = NoiseProfile.FromJson(File.ReadAllText(loadNoisePath));
                noiseProfile.Validate(sr, frameSize, hop);
                profile = noiseProfile.Magnitudes;
                Console.WriteLine($"loaded noise profile from {loadNoisePath} ({profile.Length} bins, {sr}Hz, frameSize={frameSize}, hop={hop})");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: Failed to load noise profile: {ex.Message}");
                return 1;
            }
        }
        else
        {
            int noiseLen = Math.Min(samples.Length, (int)(sr * noiseSeconds));
            if (noiseLen == 0)
            {
                Console.Error.WriteLine("Error: Not enough samples to estimate noise profile.");
                return 1;
            }

            profile = sub.EstimateNoiseProfile(samples.AsSpan(0, noiseLen));
            Console.WriteLine($"estimated noise profile from {noiseLen} samples ({noiseSeconds}s)");

            // Save noise profile if specified
            if (saveNoisePath != null)
            {
                var noiseProfile = NoiseProfile.FromEstimate(profile, sr, sub);
                File.WriteAllText(saveNoisePath, noiseProfile.ToJson(true));
                Console.WriteLine($"saved noise profile to {saveNoisePath}");
            }
        }

        var monoProgress = new Progress<double>(p => Console.WriteLine($"Progress: {p:P0}"));
        var cleaned = sub.Process(samples, profile, monoProgress);
        WavFile.WriteMono(outPath, cleaned, sr);

        Console.WriteLine($"wrote {outPath}");
        Console.WriteLine($"input RMS {Rms(samples):F5}");
        Console.WriteLine($"output RMS {Rms(cleaned):F5}");
        return 0;
    }
}

static double Rms(float[] x)
{
    double acc = 0;
    foreach (var v in x) acc += (double)v * v;
    return Math.Sqrt(acc / x.Length);
}
