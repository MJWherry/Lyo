using Lyo.EntityReference.Models;
using Lyo.Postgres;

namespace Lyo.Recipe.Postgres;

/// <summary>Settings for the recipe PostgreSQL schema.</summary>
public sealed class PostgresRecipeOptions : PostgresOptionsBase
{
    /// <summary>Configuration section this type binds from.</summary>
    public const string SectionName = "PostgresRecipe";

    /// <summary>PostgreSQL schema that owns recipe tables.</summary>
    public const string Schema = "recipe";

    /// <summary>Tenancy rules for this feature. Properties left unset come from <see cref="EntityRefOptions" />.</summary>
    /// <remarks>Recipe entities expose a nullable <c>tenant_id</c>, so any of the three tenancy modes is allowed.</remarks>
    public TenancyOptions Tenancy { get; set; } = new();

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}