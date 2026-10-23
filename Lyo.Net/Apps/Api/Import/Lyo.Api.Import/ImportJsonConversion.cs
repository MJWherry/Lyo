using Lyo.Api.Models.Enums;

namespace Lyo.Api.Import;

/// <summary>JSON produced from one import file. Field maps are applied later, by the JSON importer.</summary>
public sealed class ImportJsonConversion
{
    /// <summary>False when the file could not be turned into JSON.</summary>
    public bool IsSuccess { get; private init; }

    /// <summary>Document-level message. Null on success.</summary>
    public string? Message { get; private init; }

    /// <summary>JSON text. Empty when <see cref="IsSuccess" /> is false.</summary>
    public string Json { get; private init; } = string.Empty;

    /// <summary>How <see cref="Json" /> is shaped.</summary>
    public ImportJsonMode JsonMode { get; private init; } = ImportJsonMode.Array;

    /// <summary>Array property when <see cref="JsonMode" /> is <see cref="ImportJsonMode.Wrapped" />.</summary>
    public string? RowsPath { get; private init; }

    /// <summary>A JSON document the importer can bind.</summary>
    public static ImportJsonConversion Ok(string json, ImportJsonMode jsonMode = ImportJsonMode.Array, string? rowsPath = null)
        => new() {
            IsSuccess = true,
            Json = json,
            JsonMode = jsonMode,
            RowsPath = rowsPath
        };

    /// <summary>The file is not a document the importer can read.</summary>
    public static ImportJsonConversion Fail(string message) => new() { IsSuccess = false, Message = message };

    /// <summary>JSON from a text format. A rows path selects wrapped mode. Otherwise the root must be an array.</summary>
    public static ImportJsonConversion FromTextFormat(string json, string? rowsPath) => string.IsNullOrWhiteSpace(rowsPath) ? Ok(json) : Ok(json, ImportJsonMode.Wrapped, rowsPath);
}