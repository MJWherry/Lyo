using System.Text.Json.Serialization;

namespace Lyo.Api.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExportFormat
{
    Csv,
    Xlsx,
    Json
}