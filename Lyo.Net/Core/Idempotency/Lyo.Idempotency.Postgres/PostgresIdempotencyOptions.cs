using System.Diagnostics;
using Lyo.Postgres;

namespace Lyo.Idempotency.Postgres;

/// <summary>Connection and limits for the idempotency schema.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class PostgresIdempotencyOptions : PostgresOptionsBase
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "PostgresIdempotency";

    /// <summary>Schema that owns <c>entry</c> and the migrations history table.</summary>
    public const string Schema = "idempotency";

    /// <inheritdoc />
    protected override string SchemaName => Schema;

    /// <summary>Same limits the in-memory store uses.</summary>
    public IdempotencyOptions Limits { get; set; } = new();

    /// <inheritdoc />
    public override void Validate()
    {
        base.Validate();
        if (Limits.MaxScopeLength < 1)
            throw new ArgumentException("MaxScopeLength must be positive.", nameof(Limits));
    }

    /// <summary>Schema, auto-migrate flag, and limits. The connection string is omitted.</summary>
    public override string ToString() => $"{base.ToString()} Limits={Limits}";
}