using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Media.Models;

/// <summary>File-to-file audio convert request. Paths plus <see cref="Options" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record AudioConversionRequest
{
    /// <summary>Path or URL of the source.</summary>
    public required string InputPath { get; init; }

    /// <summary>Path or <c>pipe:1</c> of the destination.</summary>
    public required string OutputPath { get; init; }

    /// <summary>Convert knobs. Created empty when unset; empty does not force pcm.</summary>
    public AudioConversionOptions Options { get; init; } = new();

    /// <inheritdoc />
    public override string ToString() => $"AudioConversion: {InputPath} -> {OutputPath} ({Options})";

    /// <summary>Guards required paths.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(InputPath);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(OutputPath);
        ArgumentHelpers.ThrowIfNull(Options);
        Options.Validate();
    }
}
