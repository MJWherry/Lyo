using System.Diagnostics;

namespace Lyo.Email.Models;

/// <summary>One custom header on an outbound message.</summary>
/// <param name="Name">Header name.</param>
/// <param name="Value">Header value.</param>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record EmailHeader(string Name, string Value)
{
    /// <summary>Header name and value.</summary>
    public override string ToString() => $"{Name}={Value}";
}
