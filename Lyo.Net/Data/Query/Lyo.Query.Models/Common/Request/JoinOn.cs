using System.Diagnostics;

namespace Lyo.Query.Models.Common.Request;

/// <summary>One join equality: left alias.property equals right alias.property.</summary>
[DebuggerDisplay("{From} = {To}")]
public sealed class JoinOn
{
    /// <summary>Left-side path, typically <c>fromAlias.property</c>.</summary>
    public string From { get; set; } = "";

    /// <summary>Right-side path, typically <c>joinAlias.property</c>.</summary>
    public string To { get; set; } = "";
}