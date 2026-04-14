using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Lyo.Api.Services.TypeConversion;

[DebuggerDisplay("{ToString(),nq}")]
internal record EntityKeyMetadata(IReadOnlyList<IProperty> Properties, int ExpectedKeyCount)
{
    public override string ToString() => $"Count={Properties.Count} KeyTypes={string.Join(", ", Properties.Select(p => p.ClrType.Name))}";
}