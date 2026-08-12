namespace Lyo.Reporting.Web.Components;

/// <summary>Editable row for report definition parameters in <see cref="ReportParameterTable" />.</summary>
public sealed class ReportParameterEditRow
{
    public Guid? Id { get; set; }

    public string Key { get; set; } = "";

    public ReportParameterType Type { get; set; } = ReportParameterType.String;

    public string? Value { get; set; }

    public string? Description { get; set; }

    public bool Required { get; set; }

    public bool AllowMultiple { get; set; }

    public bool IsNew { get; set; }

    /// <summary>True when the stored value is encrypted server-side.</summary>
    public bool IsEncrypted { get; set; }

    /// <summary>Pipe-separated allowed values for server validation / simple select.</summary>
    public string? AllowedValues { get; set; }

    /// <summary>JSON picker source (static or root QueryReq).</summary>
    public string? Options { get; set; }

    /// <summary>Raw ciphertext when present; not round-tripped on update unless set.</summary>
    public byte[]? EncryptedValue { get; set; }
}