using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Media.Models;

/// <summary>Concat request for <see cref="IAudioConverter" />. Paths plus <see cref="Options" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record AudioConcatRequest
{
    /// <summary>Source files in order. The concat demuxer requires a matching stream layout.</summary>
    public required IReadOnlyList<string> Inputs { get; init; }

    /// <summary>Destination file.</summary>
    public required string OutputPath { get; init; }

    /// <summary>Convert knobs. Created empty when unset. Empty does not force pcm. The converter copies streams when no encoder is set.</summary>
    public AudioConversionOptions Options { get; init; } = new();

    /// <inheritdoc />
    public override string ToString() => $"AudioConcat: {Inputs.Count} -> {OutputPath}";

    /// <summary>Guards a non-empty input list and the output path.</summary>
    public void Validate()
    {
        ArgumentHelpers.ThrowIfNull(Inputs);
        ArgumentHelpers.ThrowIf(Inputs.Count == 0, "Concat requires at least one input.", nameof(Inputs));
        foreach (var input in Inputs)
            ArgumentHelpers.ThrowIfNullOrWhiteSpace(input);

        ArgumentHelpers.ThrowIfNullOrWhiteSpace(OutputPath);
        ArgumentHelpers.ThrowIfNull(Options);
        Options.Validate();
    }
}