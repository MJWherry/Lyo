namespace Lyo.Web.Automation.Postgres.Database;

/// <summary>One stored automation plan.</summary>
public sealed class AutomationPlanEntity
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Unique plan name.</summary>
    public string Name { get; set; } = "";

    /// <summary>Plan JSON, including the step list.</summary>
    public string Document { get; set; } = "";

    /// <summary>Number of steps in <see cref="Document" />.</summary>
    public int StepCount { get; set; }

    /// <summary>When the row was last written.</summary>
    public DateTime UpdatedUtc { get; set; }
}