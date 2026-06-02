using System.Diagnostics;

namespace Lyo.EntityReference.Models;

/// <summary>Subject and actor endpoints for a relation row.</summary>
/// <param name="Subject">Entity the relation applies to.</param>
/// <param name="Actor">Entity that performed or owns the relation.</param>
[DebuggerDisplay("{ToString(),nq}")]
public readonly record struct EntityRelationEndpoints(EntityRef Subject, EntityRef Actor)
{
    public override string ToString() => $"Subject={Subject}, Actor={Actor}";
}
