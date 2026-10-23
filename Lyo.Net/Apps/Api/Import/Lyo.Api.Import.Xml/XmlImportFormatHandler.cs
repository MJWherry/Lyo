using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Enums;
using Lyo.Xml;

namespace Lyo.Api.Import.Xml;

/// <summary>XML import. The document is converted to JSON. A rows path selects wrapped mode. The JSON importer applies the field map.</summary>
public sealed class XmlImportFormatHandler : IImportFormatHandler
{
    private readonly IXmlService _xml;

    /// <summary>Uses a default <see cref="XmlService" /> when <paramref name="xml" /> is omitted.</summary>
    public XmlImportFormatHandler(IXmlService? xml = null) => _xml = xml ?? new XmlService();

    /// <inheritdoc />
    public ImportFormat Format => ImportFormat.Xml;

    /// <inheritdoc />
    public ImportJsonConversion Convert(ImportRequest request)
    {
        try {
            return ImportJsonConversion.FromTextFormat(_xml.ConvertToJson(request.Content ?? string.Empty), request.RowsPath);
        }
        catch (Exception ex) {
            return ImportJsonConversion.Fail(ex.Message);
        }
    }
}