using System.Diagnostics;
using Lyo.Common.Core.Enums;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace Lyo.Common.Metadata.Records;

/// <summary>Catalog row for a circulating bill or coin; <see cref="Value" /> is in major currency units.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record CurrencyDenominationInfo(decimal Value, CurrencyDenominationKind Kind, string Name)
{
    public override string ToString() => $"{Value} ({Kind}): {Name}";
}