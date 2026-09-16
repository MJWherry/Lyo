using System.Diagnostics;
using Lyo.Exceptions;

namespace Lyo.Media.Models;

/// <summary>Concat request for <see cref="IVideoConverter" />. Paths plus <see cref="Options" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record VideoConcatRequest
{
    /// <summary>Source files in order. The concat demuxer requires a matching stream layout.</summary>
    public required IReadOnlyList<string> Inputs { get; init; }

    /// <summary>Destination file.</summary>
    public required string OutputPath { get; init; }

    /// <summary>Convert knobs. Created with stream copy when unset.</summary>
    public VideoConversionOptions Options { get; init; } = new() { CopyCodecs = true };

    /// <inheritdoc />
    public override string ToString() => $"VideoConcat: {Inputs.Count} -> {OutputPath}";

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
