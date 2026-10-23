using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SpectralDenoise;

/// <summary>
/// Rule checks for <see cref="PipelineOptions"/>.
/// </summary>
public static class PipelineOptionsValidation
{
    /// <summary>
    /// Checks every rule and returns all violations.
    /// </summary>
    /// <param name="value">Options to validate.</param>
    /// <returns>An empty list when the options are valid; otherwise one message per violated rule.</returns>
    public static IReadOnlyList<string> Validate(this PipelineOptions value)
    {
        var problems = new List<string>();

        if (value.SampleRate <= 0)
        {
            problems.Add(Format(PipelineOptionsConstants.SampleRateMessage, value.SampleRate));
        }

        if (!IsPowerOfTwo(value.FrameSize))
        {
            problems.Add(Format(PipelineOptionsConstants.FrameSizeMessage, value.FrameSize));
        }

        if (value.Hop <= 0 || value.Hop > value.FrameSize)
        {
            problems.Add(Format(PipelineOptionsConstants.HopMessage, value.Hop, value.FrameSize));
        }

        // Written so that NaN fails the check: every comparison with NaN is false.
        if (!(value.OverSubtractionFactor >= 1 && double.IsFinite(value.OverSubtractionFactor)))
        {
            problems.Add(Format(PipelineOptionsConstants.OverSubtractionFactorMessage, value.OverSubtractionFactor));
        }

        if (!(value.SpectralFloor >= 0 && value.SpectralFloor < 1))
        {
            problems.Add(Format(PipelineOptionsConstants.SpectralFloorMessage, value.SpectralFloor));
        }

        return problems;
    }

    /// <summary>
    /// Returns <c>true</c> when the options satisfy every rule.
    /// </summary>
    /// <param name="value">Options to validate.</param>
    public static bool IsValid(this PipelineOptions value)
    {
        return !Validate(value).Any();
    }

    /// <summary>
    /// Throws <see cref="InvalidPipelineOptionsException"/> listing every violated rule.
    /// </summary>
    /// <param name="value">Options to validate.</param>
    /// <exception cref="InvalidPipelineOptionsException">Thrown when one or more rules are violated.</exception>
    public static void EnsureValid(this PipelineOptions value)
    {
        IReadOnlyList<string> problems = Validate(value);

        if (problems.Count > 0)
        {
            throw new InvalidPipelineOptionsException(problems);
        }
    }

    private static bool IsPowerOfTwo(int value)
    {
        return value > 0 && (value & (value - 1)) == 0;
    }

    private static string Format(string template, params object[] args)
    {
        return string.Format(CultureInfo.InvariantCulture, template, args);
    }
}
