using System.Diagnostics;

namespace Lyo.TestApi;

[DebuggerDisplay("{ToString(),nq}")]
public record SpUniqueValueCount(string? Value, int Count);