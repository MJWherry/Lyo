using System.Reflection;
using System.Text;

namespace Lyo.Xml;

/// <summary>Writes a typed value as XML to a file, stream, string, or byte array.</summary>
public interface IXmlWriter
{
    /// <summary>Writes <paramref name="value" /> to <paramref name="xmlFilePath" />.</summary>
    void ExportToXml<T>(T value, string xmlFilePath);

    /// <summary>Writes <paramref name="value" /> to <paramref name="xmlStream" />. The stream is left open.</summary>
    void ExportToXml<T>(T value, Stream xmlStream);

    /// <summary>Serializes <paramref name="value" /> to an XML string.</summary>
    string ExportToXmlString<T>(T value);

    /// <summary>Serializes <paramref name="value" /> to bytes. A null <paramref name="encoding" /> uses the service encoding.</summary>
    byte[] ExportToXmlBytes<T>(T value, Encoding? encoding = null);

    /// <summary>Writes <paramref name="value" /> into <paramref name="destination" />. Returns false when the buffer is too small.</summary>
    bool TryExport<T>(T value, Span<byte> destination, out int bytesWritten, Encoding? encoding = null);

    /// <summary>
    /// Writes one object per item. Each field is <paramref name="fields" /> in dictionary order. <c>Order</c> does not reorder these keys. A null formatter result is an empty
    /// string. A header that is not an XML name is encoded with <see cref="System.Xml.XmlConvert.EncodeLocalName" />.
    /// </summary>
    string ExportFormattedToXmlString<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields);

    /// <summary>Writes <see cref="ExportFormattedToXmlString{T}" /> to <paramref name="xmlStream" />. The stream is left open.</summary>
    void ExportFormattedToXml<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields, Stream xmlStream);

    /// <summary>
    /// Writes one object per item. The key is the field name and the value is the property value, in dictionary order. <c>Order</c> does not reorder these keys.
    /// </summary>
    string ExportColumnsToXmlString<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns);

    /// <summary>Writes <see cref="ExportColumnsToXmlString{T}" /> to <paramref name="xmlStream" />. The stream is left open.</summary>
    void ExportColumnsToXml<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns, Stream xmlStream);

    /// <summary>
    /// Writes <paramref name="dataTable" /> as repeated item elements under the root. Header text is the element name. A blank header is <c>Column{index}</c>. A repeated header
    /// throws. Empty cells stay empty elements. A footer is the last item. A header that is not an XML name is encoded with <see cref="System.Xml.XmlConvert.EncodeLocalName" />.
    /// Naming, member order, and empty-string omission do not apply.
    /// </summary>
    string ExportToXmlStringFromDataTable(DataTable.Models.DataTable dataTable);

    /// <summary>Writes <see cref="ExportToXmlStringFromDataTable" /> to <paramref name="xmlFilePath" />.</summary>
    void ExportToXmlFromDataTable(DataTable.Models.DataTable dataTable, string xmlFilePath);

    /// <summary>Writes <see cref="ExportToXmlStringFromDataTable" /> to <paramref name="xmlStream" />. The stream is left open.</summary>
    void ExportToXmlFromDataTable(DataTable.Models.DataTable dataTable, Stream xmlStream);

    /// <summary>Serializes <paramref name="dataTable" /> to bytes. A null <paramref name="encoding" /> uses the service encoding.</summary>
    byte[] ExportToXmlBytesFromDataTable(DataTable.Models.DataTable dataTable, Encoding? encoding = null);

#if !NETSTANDARD2_0
    /// <summary>Writes <paramref name="value" /> to <paramref name="xmlFilePath" />.</summary>
    Task ExportToXmlAsync<T>(T value, string xmlFilePath, CancellationToken ct = default);

    /// <summary>Writes <paramref name="value" /> to <paramref name="xmlStream" />. The stream is left open.</summary>
    Task ExportToXmlAsync<T>(T value, Stream xmlStream, CancellationToken ct = default);

    /// <summary>Serializes <paramref name="value" /> to an XML string.</summary>
    Task<string> ExportToXmlStringAsync<T>(T value, CancellationToken ct = default);

    /// <summary>Serializes <paramref name="value" /> to UTF-8 XML bytes.</summary>
    Task<byte[]> ExportToXmlBytesAsync<T>(T value, CancellationToken ct = default);

    /// <summary>Writes <see cref="ExportFormattedToXmlString{T}" /> to <paramref name="xmlStream" />. The stream is left open.</summary>
    Task ExportFormattedToXmlAsync<T>(IEnumerable<T> items, IReadOnlyDictionary<string, Func<T, string>> fields, Stream xmlStream, CancellationToken ct = default);

    /// <summary>Writes <see cref="ExportColumnsToXmlString{T}" /> to <paramref name="xmlStream" />. The stream is left open.</summary>
    Task ExportColumnsToXmlAsync<T>(IEnumerable<T> items, IReadOnlyDictionary<string, PropertyInfo> columns, Stream xmlStream, CancellationToken ct = default);

    /// <summary>Writes <see cref="ExportToXmlStringFromDataTable" /> to <paramref name="xmlFilePath" />.</summary>
    Task ExportToXmlFromDataTableAsync(DataTable.Models.DataTable dataTable, string xmlFilePath, CancellationToken ct = default);

    /// <summary>Writes <see cref="ExportToXmlStringFromDataTable" /> to <paramref name="xmlStream" />. The stream is left open.</summary>
    Task ExportToXmlFromDataTableAsync(DataTable.Models.DataTable dataTable, Stream xmlStream, CancellationToken ct = default);

    /// <summary>Serializes <paramref name="dataTable" /> to an XML string.</summary>
    Task<string> ExportToXmlStringFromDataTableAsync(DataTable.Models.DataTable dataTable, CancellationToken ct = default);

    /// <summary>Serializes <paramref name="dataTable" /> to bytes. A null <paramref name="encoding" /> uses the service encoding.</summary>
    Task<byte[]> ExportToXmlBytesFromDataTableAsync(DataTable.Models.DataTable dataTable, Encoding? encoding = null, CancellationToken ct = default);
#endif
}
