using System.Diagnostics;
using Lyo.Common.Core.Extensions;
using Lyo.Parameters;

namespace Lyo.Job.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record JobParameterRes(
    Guid Id,
    Guid JobDefinitionId,
    string Key,
    string? Description,
    string Type,
    string? Value,
    byte[]? EncryptedValue,
    bool Enabled,
    bool Required,
    string? ValidationRegex = null,
    int? MinLength = null,
    int? MaxLength = null,
    string? AllowedValues = null,
    string? Options = null,
    int Order = 0,
    LyoParameterDefaultKind DefaultKind = LyoParameterDefaultKind.Literal,
    string? DefaultTemplate = null) : ILyoParameterDefinition
{
    public override string ToString() => $"Id={Id.Truncated(4, 4)} DefinitionId={JobDefinitionId.Truncated()} ({Type}) {Key}={Value} ({Description})";
}
