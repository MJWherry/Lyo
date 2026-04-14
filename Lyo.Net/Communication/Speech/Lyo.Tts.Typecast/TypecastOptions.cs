using System.Diagnostics;
using Lyo.Tts.Models;
using Lyo.Typecast.Client.Enums;

namespace Lyo.Tts.Typecast;

/// <summary>Configuration options for Typecast TTS service.</summary>
/// <remarks>
/// <para>This class is not thread-safe. Options should be configured during application startup and not modified after service registration.</para>
/// <para>All properties from the base <see cref="TtsServiceOptions" /> class are also available.</para>
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class TypecastOptions : TtsServiceOptions
{
    /// <summary>The default configuration section name for TypecastOptions.</summary>
    public const string SectionName = "TypecastOptions";

    /// <summary>Gets or sets the default model to use for synthesis. Defaults to SsfmV30.</summary>
    public string DefaultModel { get; set; } = TypecastModel.SsfmV30;

    /// <summary>Returns a string representation of the options.</summary>
    /// <returns>A string containing the DefaultModel.</returns>
    public override string ToString() => $"{base.ToString()} DefaultModel={DefaultModel}";
}