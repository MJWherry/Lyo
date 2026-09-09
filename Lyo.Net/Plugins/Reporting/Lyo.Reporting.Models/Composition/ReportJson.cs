using System.Text.Json;
using System.Text.Json.Serialization;
using Lyo.Common.Json.JsonConverters;
using Lyo.Exceptions;
using Lyo.Reporting.Models.Models;

namespace Lyo.Reporting.Models.Composition;

/// <summary>Shared JSON contract for <see cref="Report{T}" /> composition payloads stored in <c>ReportDataJson</c>.</summary>
public static class ReportJson
{
    /// <summary>Options used by renderers, the composition processor, and the design workbench.</summary>
    public static JsonSerializerOptions Options { get; } = new() {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new ObjectJsonConverter() }
    };

    /// <summary>Deserializes composition JSON into a <see cref="Report{T}" />.</summary>
    public static Report<T> Deserialize<T>(string json)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(json);
        return JsonSerializer.Deserialize<Report<T>>(json, Options) ?? throw new ReportValidationException("Failed to deserialize report JSON.");
    }

    /// <summary>Serializes a report using the shared composition options.</summary>
    public static string Serialize<T>(Report<T> report)
    {
        ArgumentHelpers.ThrowIfNull(report);
        return JsonSerializer.Serialize(report, Options);
    }
}
