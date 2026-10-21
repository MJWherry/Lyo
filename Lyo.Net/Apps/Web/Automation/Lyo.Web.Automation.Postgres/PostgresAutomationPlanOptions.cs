using Lyo.Postgres;

namespace Lyo.Web.Automation.Postgres;

/// <summary>Settings for the automation plan database.</summary>
public sealed class PostgresAutomationPlanOptions : PostgresOptionsBase
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "PostgresAutomationPlans";

    /// <summary>PostgreSQL schema for plan rows.</summary>
    public const string Schema = "automation";

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}