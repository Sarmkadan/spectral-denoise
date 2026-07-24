using System;
using SpectralDenoise;

Console.WriteLine("Testing SpectralSubtractor validation...");

// Test 1: Valid configuration
try
{
    var valid = new SpectralSubtractor(frameSize: 1024, hop: 256)
    {
        Alpha = 2.0,
        Beta = 0.02
    };

    var validationResult = valid.Validate();
    Console.WriteLine($"✓ Valid config passed validation: {validationResult.Count} problems");

    if (validationResult.Count > 0)
    {
        Console.WriteLine("Problems found:");
        foreach (var problem in validationResult)
        {
            Console.WriteLine($"  - {problem}");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Valid config failed: {ex.Message}");
}

// Test 2: Invalid frame size (not power of two)
try
{
    var invalid1 = new SpectralSubtractor(frameSize: 1000, hop: 256);
    invalid1.EnsureValid();
    Console.WriteLine("✗ Invalid frame size (1000) should have failed but didn't");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"✓ Invalid frame size correctly rejected: {ex.Message.Substring(0, Math.Min(50, ex.Message.Length))}...");
}

// Test 3: Invalid Alpha (< 1.0)
try
{
    var invalid2 = new SpectralSubtractor(frameSize: 1024, hop: 256)
    {
        Alpha = 0.5,
        Beta = 0.02
    };
    invalid2.EnsureValid();
    Console.WriteLine("✗ Invalid Alpha (0.5) should have failed but didn't");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"✓ Invalid Alpha correctly rejected: {ex.Message.Substring(0, Math.Min(50, ex.Message.Length))}...");
}

// Test 4: Invalid Beta (> 1.0)
try
{
    var invalid3 = new SpectralSubtractor(frameSize: 1024, hop: 256)
    {
        Alpha = 2.0,
        Beta = 1.5
    };
    invalid3.EnsureValid();
    Console.WriteLine("✗ Invalid Beta (1.5) should have failed but didn't");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"✓ Invalid Beta correctly rejected: {ex.Message.Substring(0, Math.Min(50, ex.Message.Length))}...");
}

// Test 5: Frame size too small
try
{
    var invalid4 = new SpectralSubtractor(frameSize: 64, hop: 32);
    invalid4.EnsureValid();
    Console.WriteLine("✗ Small frame size (64) should have failed but didn't");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"✓ Small frame size correctly rejected: {ex.Message.Substring(0, Math.Min(50, ex.Message.Length))}...");
}

// Test 6: Frame size too large
try
{
    var invalid5 = new SpectralSubtractor(frameSize: 16384, hop: 4096);
    invalid5.EnsureValid();
    Console.WriteLine("✗ Large frame size (16384) should have failed but didn't");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"✓ Large frame size correctly rejected: {ex.Message.Substring(0, Math.Min(50, ex.Message.Length))}...");
}

Console.WriteLine("\nAll validation tests completed!");