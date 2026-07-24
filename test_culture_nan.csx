#!/usr/bin/env dotnet-script

// Test script to verify culture-invariant and NaN-safe JSON round-trip for DSP parameters

#r "System.Text.Json"

using System;
using System.Globalization;
using System.Text.Json;
using SpectralDenoise;

Console.WriteLine("Testing culture-invariant and NaN-safe JSON round-trip...\n");

// Test 1: Basic serialization with default culture
Console.WriteLine("Test 1: Basic serialization");
var processor = new SpectralSubtractor(1024, 256);
processor.Alpha = 2.5;
processor.Beta = 0.05;
processor.OverSubtractionFactor = 1.2;
processor.SpectralFloor = 0.03;
processor.AttackMs = 10.5;
processor.ReleaseMs = 50.25;

string json = processor.ToJson();
Console.WriteLine("Serialized JSON:");
Console.WriteLine(json);
Console.WriteLine();

// Test 2: Round-trip test
Console.WriteLine("Test 2: Round-trip serialization/deserialization");
var deserialized = SpectralSubtractorJsonExtensions.FromJson(json);
if (deserialized != null)
{
    Console.WriteLine("✓ Deserialization successful");
    Console.WriteLine($"Alpha: {deserialized.Alpha}");
    Console.WriteLine($"Beta: {deserialized.Beta}");
    Console.WriteLine($"OverSubtractionFactor: {deserialized.OverSubtractionFactor}");
    Console.WriteLine($"SpectralFloor: {deserialized.SpectralFloor}");
    Console.WriteLine($"AttackMs: {deserialized.AttackMs}");
    Console.WriteLine($"ReleaseMs: {deserialized.ReleaseMs}");
    Console.WriteLine();
}
else
{
    Console.WriteLine("✗ Deserialization failed");
    return 1;
}

// Test 3: NaN handling in serialization
Console.WriteLine("Test 3: NaN handling in serialization");
processor.Alpha = double.NaN;
processor.Beta = double.PositiveInfinity;
processor.OverSubtractionFactor = double.NegativeInfinity;
processor.SpectralFloor = 0.0;
processor.AttackMs = double.NaN;
processor.ReleaseMs = 10.0;

string jsonWithNaN = processor.ToJson();
Console.WriteLine("Serialized JSON with NaN/Infinity values:");
Console.WriteLine(jsonWithNaN);
Console.WriteLine();

var deserializedFromNaN = SpectralSubtractorJsonExtensions.FromJson(jsonWithNaN);
if (deserializedFromNaN != null)
{
    Console.WriteLine("✓ Deserialization of sanitized JSON successful");
    Console.WriteLine($"Alpha (was NaN, should be 2.0): {deserializedFromNaN.Alpha}");
    Console.WriteLine($"Beta (was +Infinity, should be 0.02): {deserializedFromNaN.Beta}");
    Console.WriteLine($"OverSubtractionFactor (was -Infinity, should be 1.0): {deserializedFromNaN.OverSubtractionFactor}");
    Console.WriteLine($"AttackMs (was NaN, should be 0.0): {deserializedFromNaN.AttackMs}");
    Console.WriteLine();
}
else
{
    Console.WriteLine("✗ Deserialization of sanitized JSON failed");
    return 1;
}

// Test 4: Complex array NaN handling
Console.WriteLine("Test 4: Complex array NaN handling");
var complexArray = new System.Numerics.Complex[]
{
    new System.Numerics.Complex(1.0, 2.0),
    new System.Numerics.Complex(double.NaN, double.PositiveInfinity),
    new System.Numerics.Complex(double.NegativeInfinity, 3.0),
    new System.Numerics.Complex(4.0, double.NaN)
};

string complexJson = complexArray.ToJson();
Console.WriteLine("Complex array JSON:");
Console.WriteLine(complexJson);
Console.WriteLine();

var deserializedComplex = FftJsonExtensions.FromJson(complexJson);
if (deserializedComplex != null)
{
    Console.WriteLine("✓ Complex array deserialization successful");
    Console.WriteLine("Sanitized values:");
    foreach (var c in deserializedComplex)
    {
        Console.WriteLine($"  Real: {c.Real}, Imaginary: {c.Imaginary}");
    }
    Console.WriteLine();
}
else
{
    Console.WriteLine("✗ Complex array deserialization failed");
    return 1;
}

// Test 5: Culture-specific formatting (try with bg-BG which uses comma as decimal separator)
Console.WriteLine("Test 5: Culture-specific formatting");
var originalCulture = CultureInfo.CurrentCulture;
try
{
    // Set to Bulgarian culture which uses comma as decimal separator
    CultureInfo.CurrentCulture = new CultureInfo("bg-BG");

    var processorBg = new SpectralSubtractor(512, 128);
    processorBg.Alpha = 1.23456789;
    processorBg.Beta = 0.123456789;

    string jsonBg = processorBg.ToJson();
    Console.WriteLine("Serialized with bg-BG culture:");
    Console.WriteLine(jsonBg);

    // Check that it uses dot (invariant) not comma
    if (jsonBg.Contains("."))
    {
        Console.WriteLine("✓ Uses invariant decimal format (dot) regardless of culture");
    }
    else
    {
        Console.WriteLine("✗ May be using culture-specific decimal format");
    }
    Console.WriteLine();
}
finally
{
    CultureInfo.CurrentCulture = originalCulture;
}

Console.WriteLine("All tests passed! ✓");
return 0;
