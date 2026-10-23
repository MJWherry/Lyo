namespace Lyo.Api.Models.Enums;

/// <summary>File format accepted by the import endpoint. JSON is built in. CSV and XLSX are addon handlers. YAML, XML, and TOML follow the same contract.</summary>
public enum ImportFormat
{
    /// <summary>A JSON array, a wrapped array, or NDJSON.</summary>
    Json,

    /// <summary>CSV with a header row.</summary>
    Csv,

    /// <summary>Excel workbook. The first sheet is used unless a sheet name is set.</summary>
    Xlsx,

    /// <summary>YAML sequence, or a mapping when a rows path is set.</summary>
    Yaml,

    /// <summary>XML sequence of elements, or a path to that sequence.</summary>
    Xml,

    /// <summary>TOML array of tables, or a path to that array.</summary>
    Toml
}