using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SpectralDenoise;

/// <summary>
/// Thrown when <see cref="PipelineOptions"/> violate one or more rules. Carries every violation found,
/// not just the first one.
/// </summary>
public sealed class InvalidPipelineOptionsException : ArgumentException
{
    /// <summary>
    /// Initializes a new instance with the list of violations.
    /// </summary>
    /// <param name="errors">One human-readable message per violated rule. Must not be empty.</param>
    public InvalidPipelineOptionsException(IReadOnlyList<string> errors)
        : base(FormatMessage(errors))
    {
        Errors = errors.ToArray();
    }

    /// <summary>
    /// Gets every rule violation, in the order the rules are checked.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    private static string FormatMessage(IReadOnlyList<string> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var header = string.Format(CultureInfo.InvariantCulture, PipelineOptionsConstants.InvalidOptionsHeader, errors.Count);
        return header + Environment.NewLine + string.Join(Environment.NewLine, errors.Select(error => "  - " + error));
    }
}
