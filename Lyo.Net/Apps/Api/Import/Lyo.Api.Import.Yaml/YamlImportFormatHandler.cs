using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Enums;
using Lyo.Yaml;

namespace Lyo.Api.Import.Yaml;

/// <summary>YAML import. The document is converted to JSON. A rows path selects wrapped mode. The JSON importer applies the field map.</summary>
public sealed class YamlImportFormatHandler : IImportFormatHandler
{
    private readonly IYamlService _yaml;

    /// <summary>Uses a default <see cref="YamlService" /> when <paramref name="yaml" /> is omitted.</summary>
    public YamlImportFormatHandler(IYamlService? yaml = null) => _yaml = yaml ?? new YamlService();

    /// <inheritdoc />
    public ImportFormat Format => ImportFormat.Yaml;

    /// <inheritdoc />
    public ImportJsonConversion Convert(ImportRequest request)
    {
        try {
            return ImportJsonConversion.FromTextFormat(_yaml.ConvertToJson(request.Content ?? string.Empty), request.RowsPath);
        }
        catch (Exception ex) {
            return ImportJsonConversion.Fail(ex.Message);
        }
    }
}