using System.Text;
using Lyo.Result;

namespace Lyo.Json;

/// <summary>Reads a JSON document into a typed value from a file, stream, byte array, string, or span.</summary>
public interface IJsonReader
{
    /// <summary>Reads <paramref name="jsonFilePath" /> as <typeparamref name="T" /> using the service encoding. A BOM wins.</summary>
    T ParseFile<T>(string jsonFilePath);

    /// <summary>Reads <paramref name="jsonStream" /> as <typeparamref name="T" />. The stream is left open. A BOM wins over the service encoding.</summary>
    T ParseStream<T>(Stream jsonStream);

    /// <summary>Reads <paramref name="jsonBytes" /> as <typeparamref name="T" />. A null <paramref name="encoding" /> uses the service encoding.</summary>
    T ParseBytes<T>(byte[] jsonBytes, Encoding? encoding = null);

    /// <summary>Reads <paramref name="json" /> as <typeparamref name="T" />. A null <paramref name="encoding" /> uses the service encoding.</summary>
    T Parse<T>(ReadOnlySpan<byte> json, Encoding? encoding = null);

    /// <summary>Reads already-decoded <paramref name="json" /> as <typeparamref name="T" />.</summary>
    T Parse<T>(ReadOnlySpan<char> json);

    /// <summary>Reads <paramref name="json" /> as <typeparamref name="T" />.</summary>
    T ParseString<T>(string json);

    /// <summary>Reads one JSON value per line from <paramref name="jsonFilePath" />. Blank lines are skipped.</summary>
    List<T> ParseNdjsonFile<T>(string jsonFilePath);

    /// <summary>Reads one JSON value per line from <paramref name="jsonStream" />. The stream is left open. Blank lines are skipped.</summary>
    List<T> ParseNdjsonStream<T>(Stream jsonStream);

    /// <summary>Reads one JSON value per line from <paramref name="ndjson" />. Blank lines are skipped.</summary>
    List<T> ParseNdjsonString<T>(string ndjson);

    /// <summary>Reads one JSON value per line from UTF-8 <paramref name="ndjson" />. Blank lines are skipped.</summary>
    List<T> ParseNdjsonBytes<T>(byte[] ndjson);

    /// <summary>
    /// Walks a JSON array element by element. One bad element does not drop the rest. A root object is read only when <paramref name="rowsPath" /> names an array property.
    /// JSON <c>null</c>, an empty document, and a root object without that path fail the document.
    /// </summary>
    JsonImportResult<T> ParseArrayAsResult<T>(string json, string? rowsPath = null);

    /// <summary>Reads one JSON value per line. A bad line is an issue. Blank lines are skipped and do not consume an index.</summary>
    JsonImportResult<T> ParseNdjsonAsResult<T>(string ndjson);

    /// <summary>
    /// Reads a JSON array of objects into a data table. <paramref name="hasFooterRow" /> moves the last object into the footer. A root that is not an array of objects, or a
    /// nested cell, fails the result. Cell text stays text.
    /// </summary>
    Result<DataTable.Models.DataTable> ParseStringAsDataTable(string json, bool hasFooterRow = false);

    /// <summary>Reads <paramref name="jsonBytes" /> as a data table. A null <paramref name="encoding" /> uses the service encoding.</summary>
    Result<DataTable.Models.DataTable> ParseBytesAsDataTable(byte[] jsonBytes, bool hasFooterRow = false, Encoding? encoding = null);

    /// <summary>Reads <paramref name="jsonStream" /> as a data table. The stream is left open.</summary>
    Result<DataTable.Models.DataTable> ParseStreamAsDataTable(Stream jsonStream, bool hasFooterRow = false);

    /// <summary>Reads <paramref name="jsonFilePath" /> as a data table.</summary>
    Result<DataTable.Models.DataTable> ParseFileAsDataTable(string jsonFilePath, bool hasFooterRow = false);

#if !NETSTANDARD2_0
    /// <summary>Reads <paramref name="jsonFilePath" /> as <typeparamref name="T" />.</summary>
    Task<T> ParseFileAsync<T>(string jsonFilePath, CancellationToken ct = default);

    /// <summary>Reads <paramref name="jsonStream" /> as <typeparamref name="T" />. The stream is left open.</summary>
    Task<T> ParseStreamAsync<T>(Stream jsonStream, CancellationToken ct = default);

    /// <summary>Reads <paramref name="jsonBytes" /> as <typeparamref name="T" />. A null <paramref name="encoding" /> uses the service encoding.</summary>
    Task<T> ParseBytesAsync<T>(byte[] jsonBytes, Encoding? encoding = null, CancellationToken ct = default);

    /// <summary>Reads <paramref name="json" /> as <typeparamref name="T" />.</summary>
    Task<T> ParseStringAsync<T>(string json, CancellationToken ct = default);

    /// <summary>Yields one JSON value per line from <paramref name="jsonFilePath" />. Blank lines are skipped.</summary>
    IAsyncEnumerable<T> ParseNdjsonFileAsync<T>(string jsonFilePath, CancellationToken ct = default);

    /// <summary>Yields one JSON value per line from <paramref name="jsonStream" />. The stream is left open. Blank lines are skipped.</summary>
    IAsyncEnumerable<T> ParseNdjsonStreamAsync<T>(Stream jsonStream, CancellationToken ct = default);

    /// <summary>Reads <paramref name="json" /> as a data table.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseStringAsDataTableAsync(string json, bool hasFooterRow = false, CancellationToken ct = default);

    /// <summary>Reads <paramref name="jsonBytes" /> as a data table. A null <paramref name="encoding" /> uses the service encoding.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseBytesAsDataTableAsync(byte[] jsonBytes, bool hasFooterRow = false, Encoding? encoding = null, CancellationToken ct = default);

    /// <summary>Reads <paramref name="jsonStream" /> as a data table. The stream is left open.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseStreamAsDataTableAsync(Stream jsonStream, bool hasFooterRow = false, CancellationToken ct = default);

    /// <summary>Reads <paramref name="jsonFilePath" /> as a data table.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseFileAsDataTableAsync(string jsonFilePath, bool hasFooterRow = false, CancellationToken ct = default);
#endif
}
