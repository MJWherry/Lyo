using System.Text;
using Lyo.Result;

namespace Lyo.Xml;

/// <summary>Reads an XML document into a typed value from a file, stream, byte array, or string.</summary>
public interface IXmlReader
{
    /// <summary>Reads <paramref name="xmlFilePath" /> as <typeparamref name="T" />.</summary>
    T ParseFile<T>(string xmlFilePath);

    /// <summary>Reads <paramref name="xmlStream" /> as <typeparamref name="T" />. The stream is left open.</summary>
    T ParseStream<T>(Stream xmlStream);

    /// <summary>Reads <paramref name="xmlBytes" /> as <typeparamref name="T" />. A null <paramref name="encoding" /> uses the service encoding.</summary>
    T ParseBytes<T>(byte[] xmlBytes, Encoding? encoding = null);

    /// <summary>Reads <paramref name="xml" /> as <typeparamref name="T" />. A null <paramref name="encoding" /> uses the service encoding.</summary>
    T Parse<T>(ReadOnlySpan<byte> xml, Encoding? encoding = null);

    /// <summary>Reads already-decoded <paramref name="xml" /> as <typeparamref name="T" />.</summary>
    T Parse<T>(ReadOnlySpan<char> xml);

    /// <summary>Reads <paramref name="xml" /> as <typeparamref name="T" />.</summary>
    T ParseString<T>(string xml);

    /// <summary>
    /// Reads repeated item elements into a data table. <paramref name="hasFooterRow" /> moves the last item into the footer. Element text stays text. Encoded local names are
    /// decoded. A nested element fails the result.
    /// </summary>
    Result<DataTable.Models.DataTable> ParseStringAsDataTable(string xml, bool hasFooterRow = false);

    /// <summary>Reads <paramref name="xmlBytes" /> as a data table. A null <paramref name="encoding" /> uses the service encoding.</summary>
    Result<DataTable.Models.DataTable> ParseBytesAsDataTable(byte[] xmlBytes, bool hasFooterRow = false, Encoding? encoding = null);

    /// <summary>Reads <paramref name="xmlStream" /> as a data table. The stream is left open.</summary>
    Result<DataTable.Models.DataTable> ParseStreamAsDataTable(Stream xmlStream, bool hasFooterRow = false);

    /// <summary>Reads <paramref name="xmlFilePath" /> as a data table.</summary>
    Result<DataTable.Models.DataTable> ParseFileAsDataTable(string xmlFilePath, bool hasFooterRow = false);

#if !NETSTANDARD2_0
    /// <summary>Reads <paramref name="xmlFilePath" /> as <typeparamref name="T" />.</summary>
    Task<T> ParseFileAsync<T>(string xmlFilePath, CancellationToken ct = default);

    /// <summary>Reads <paramref name="xmlStream" /> as <typeparamref name="T" />. The stream is left open.</summary>
    Task<T> ParseStreamAsync<T>(Stream xmlStream, CancellationToken ct = default);

    /// <summary>Reads UTF-8 <paramref name="xmlBytes" /> as <typeparamref name="T" />.</summary>
    Task<T> ParseBytesAsync<T>(byte[] xmlBytes, CancellationToken ct = default);

    /// <summary>Reads <paramref name="xml" /> as <typeparamref name="T" />.</summary>
    Task<T> ParseStringAsync<T>(string xml, CancellationToken ct = default);

    /// <summary>Reads <paramref name="xml" /> as a data table.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseStringAsDataTableAsync(string xml, bool hasFooterRow = false, CancellationToken ct = default);

    /// <summary>Reads <paramref name="xmlBytes" /> as a data table. A null <paramref name="encoding" /> uses the service encoding.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseBytesAsDataTableAsync(byte[] xmlBytes, bool hasFooterRow = false, Encoding? encoding = null, CancellationToken ct = default);

    /// <summary>Reads <paramref name="xmlStream" /> as a data table. The stream is left open.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseStreamAsDataTableAsync(Stream xmlStream, bool hasFooterRow = false, CancellationToken ct = default);

    /// <summary>Reads <paramref name="xmlFilePath" /> as a data table.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseFileAsDataTableAsync(string xmlFilePath, bool hasFooterRow = false, CancellationToken ct = default);
#endif
}
