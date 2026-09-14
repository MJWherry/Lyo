using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Tts.Models;
using Lyo.Typecast.Client.Enums;

namespace Lyo.Tts.Typecast;

/// <summary>Settings for the Typecast TTS provider.</summary>
/// <remarks>
/// <para>Not thread-safe. Configure during startup and leave the instance alone after it is registered.</para>
/// <para>Inherited members from <see cref="TtsServiceOptions" /> remain available.</para>
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class TypecastOptions : TtsServiceOptions
{
    /// <summary>Default config section used for TypecastOptions.</summary>
    public const string SectionName = "TypecastOptions";

    /// <summary>Default synthesis model. Defaults to SsfmV30.</summary>
    public string DefaultModel { get; set; } = TypecastModel.SsfmV30;

    /// <inheritdoc />
    public override void Validate()
    {
        base.Validate();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(DefaultModel);
    }

    /// <summary>String form of the options.</summary>
    /// <returns>A string that includes DefaultModel.</returns>
    public override string ToString() => $"{base.ToString()} DefaultModel={DefaultModel}";
}