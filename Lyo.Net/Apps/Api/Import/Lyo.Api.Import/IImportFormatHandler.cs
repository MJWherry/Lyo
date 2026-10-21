using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Enums;

namespace Lyo.Api.Import;

/// <summary>Turns one file format into JSON. The JSON importer applies <see cref="ImportRequest.Fields" /> and binds rows.</summary>
public interface IImportFormatHandler
{
    /// <summary>Format this handler converts.</summary>
    ImportFormat Format { get; }

    /// <summary>Converts <paramref name="request" /> to JSON. Does not apply the field map and does not bind a CLR type.</summary>
    ImportJsonConversion Convert(ImportRequest request);
}