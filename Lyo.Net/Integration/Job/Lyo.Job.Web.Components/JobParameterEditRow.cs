namespace Lyo.Job.Web.Components;

/// <summary>
/// Editable row shared by the compact parameter tables (<see cref="JobParameterTable" />) used for definition parameters, schedule parameters, and run parameters. Which flag
/// columns apply (Required / Allow Multiple / Enabled) is decided by the hosting table.
/// </summary>
public class JobParameterEditRow
{
    public Guid? Id { get; set; }

    public string Key { get; set; } = "";

    public JobParameterType Type { get; set; } = JobParameterType.String;

    public string? Value { get; set; }

    public string? Description { get; set; }

    public bool Required { get; set; }

    public bool AllowMultiple { get; set; }

    public bool Enabled { get; set; } = true;

    public bool IsNew { get; set; }

    /// <summary>True when the stored value is encrypted server-side; the value cell shows a warning chip instead of the raw value.</summary>
    public bool IsEncrypted { get; set; }
}