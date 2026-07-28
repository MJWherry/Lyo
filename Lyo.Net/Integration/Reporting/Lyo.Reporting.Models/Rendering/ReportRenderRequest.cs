using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Models.Response;

namespace Lyo.Reporting.Models.Rendering;

/// <summary>Input for <see cref="IReportRenderer.RenderAsync" />.</summary>
public sealed class ReportRenderRequest
{
    /// <summary>Serialized <see cref="Models.Report{T}" /> JSON.</summary>
    public string ReportDataJson { get; init; } = null!;

    public ReportFormat Format { get; init; }

    /// <summary>Absolute path where the renderer should write the output file (typically from IoTemp).</summary>
    public string OutputFilePath { get; init; } = null!;

    public string? SuggestedFileName { get; init; }

    public IReadOnlyList<ReportGenerationParameterRes> Parameters { get; init; } = [];

    /// <summary>JSON object map of Key→Value synthesized from <see cref="Parameters" />.</summary>
    public string? ParametersJson { get; init; }

    public IServiceProvider? Services { get; init; }
}