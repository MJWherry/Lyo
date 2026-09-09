using Lyo.Query.Models.Common;

namespace Lyo.Validation.Models;

/// <summary>
/// Serializable validation document: a named <see cref="WhereClause" /> tree that an instance must match. Hosts persist and exchange this DTO (API JSON, Postgres JSONB); compile
/// it with <see cref="IValidationSchemaCompiler" />.
/// </summary>
public sealed class ValidationSchema
{
    /// <summary>Lookup key (for example <c>signup.v2</c>). Required and unique per store.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Optional CLR type name (<see cref="Type.Name" /> or <see cref="Type.FullName" />) checked when compiling to <c>IValidator&lt;T&gt;</c>.</summary>
    public string? TargetTypeName { get; set; }

    /// <summary>Optional human-readable schema description (not evaluated).</summary>
    public string? Description { get; set; }

    /// <summary>Constraints the instance must satisfy. Valid only when the WhereClause engine reports a match.</summary>
    public WhereClause Constraints { get; set; } = null!;

    /// <summary>Optional error code/message overrides keyed by dotted field path (ordinal ignore-case at apply time).</summary>
    public IReadOnlyDictionary<string, ValidationMessage>? Messages { get; set; }
}
