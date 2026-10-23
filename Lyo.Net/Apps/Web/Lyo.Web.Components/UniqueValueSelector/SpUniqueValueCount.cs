using System.Diagnostics;

namespace Lyo.Web.Components.UniqueValueSelector;

[DebuggerDisplay("{ToString(),nq}")]
public record SpUniqueValueCount(string? Value, int Count);