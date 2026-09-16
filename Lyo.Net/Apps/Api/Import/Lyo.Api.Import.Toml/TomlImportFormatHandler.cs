using Lyo.Api.Import;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Enums;
using Lyo.Toml;

namespace Lyo.Api.Import.Toml;

/// <summary>TOML import. The document is converted to JSON. An array of tables needs a rows path. The JSON importer applies the field map.</summary>
public sealed class TomlImportFormatHandler : IImportFormatHandler
{
    private readonly ITomlService _toml;

    /// <summary>Uses a default <see cref="TomlService" /> when <paramref name="toml" /> is omitted.</summary>
    public TomlImportFormatHandler(ITomlService? toml = null) => _toml = toml ?? new TomlService();

    /// <inheritdoc />
    public ImportFormat Format => ImportFormat.Toml;

    /// <inheritdoc />
    public ImportJsonConversion Convert(ImportRequest request)
    {
        try {
            return ImportJsonConversion.FromTextFormat(_toml.ConvertToJson(request.Content ?? string.Empty), request.RowsPath);
        }
        catch (Exception ex) {
            return ImportJsonConversion.Fail(ex.Message);
        }
    }
}
