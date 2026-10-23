using System.Text;
using Lyo.Result;

namespace Lyo.Yaml;

/// <summary>Reads a YAML document into a typed value from a file, stream, byte array, or string.</summary>
public interface IYamlReader
{
    /// <summary>Reads <paramref name="yamlFilePath" /> as <typeparamref name="T" />.</summary>
    T ParseFile<T>(string yamlFilePath);

    /// <summary>Reads <paramref name="yamlStream" /> as <typeparamref name="T" />. The stream is left open.</summary>
    T ParseStream<T>(Stream yamlStream);

    /// <summary>Reads <paramref name="yamlBytes" /> as <typeparamref name="T" />. A null <paramref name="encoding" /> uses the service encoding.</summary>
    T ParseBytes<T>(byte[] yamlBytes, Encoding? encoding = null);

    /// <summary>Reads <paramref name="yaml" /> as <typeparamref name="T" />. A null <paramref name="encoding" /> uses the service encoding.</summary>
    T Parse<T>(ReadOnlySpan<byte> yaml, Encoding? encoding = null);

    /// <summary>Reads already-decoded <paramref name="yaml" /> as <typeparamref name="T" />.</summary>
    T Parse<T>(ReadOnlySpan<char> yaml);

    /// <summary>Reads <paramref name="yaml" /> as <typeparamref name="T" />.</summary>
    T ParseString<T>(string yaml);

    /// <summary>
    /// Reads a sequence of maps into a data table. <paramref name="hasFooterRow" /> moves the last map into the footer. A root that is not a sequence of maps, or a nested cell,
    /// fails the result. Cell text stays text.
    /// </summary>
    Result<DataTable.Models.DataTable> ParseStringAsDataTable(string yaml, bool hasFooterRow = false);

    /// <summary>Reads <paramref name="yamlBytes" /> as a data table. A null <paramref name="encoding" /> uses the service encoding.</summary>
    Result<DataTable.Models.DataTable> ParseBytesAsDataTable(byte[] yamlBytes, bool hasFooterRow = false, Encoding? encoding = null);

    /// <summary>Reads <paramref name="yamlStream" /> as a data table. The stream is left open.</summary>
    Result<DataTable.Models.DataTable> ParseStreamAsDataTable(Stream yamlStream, bool hasFooterRow = false);

    /// <summary>Reads <paramref name="yamlFilePath" /> as a data table.</summary>
    Result<DataTable.Models.DataTable> ParseFileAsDataTable(string yamlFilePath, bool hasFooterRow = false);

#if !NETSTANDARD2_0
    /// <summary>Reads <paramref name="yamlFilePath" /> as <typeparamref name="T" />.</summary>
    Task<T> ParseFileAsync<T>(string yamlFilePath, CancellationToken ct = default);

    /// <summary>Reads <paramref name="yamlStream" /> as <typeparamref name="T" />. The stream is left open.</summary>
    Task<T> ParseStreamAsync<T>(Stream yamlStream, CancellationToken ct = default);

    /// <summary>Reads UTF-8 <paramref name="yamlBytes" /> as <typeparamref name="T" />.</summary>
    Task<T> ParseBytesAsync<T>(byte[] yamlBytes, CancellationToken ct = default);

    /// <summary>Reads <paramref name="yaml" /> as <typeparamref name="T" />.</summary>
    Task<T> ParseStringAsync<T>(string yaml, CancellationToken ct = default);

    /// <summary>Reads <paramref name="yaml" /> as a data table.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseStringAsDataTableAsync(string yaml, bool hasFooterRow = false, CancellationToken ct = default);

    /// <summary>Reads <paramref name="yamlBytes" /> as a data table. A null <paramref name="encoding" /> uses the service encoding.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseBytesAsDataTableAsync(byte[] yamlBytes, bool hasFooterRow = false, Encoding? encoding = null, CancellationToken ct = default);

    /// <summary>Reads <paramref name="yamlStream" /> as a data table. The stream is left open.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseStreamAsDataTableAsync(Stream yamlStream, bool hasFooterRow = false, CancellationToken ct = default);

    /// <summary>Reads <paramref name="yamlFilePath" /> as a data table.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseFileAsDataTableAsync(string yamlFilePath, bool hasFooterRow = false, CancellationToken ct = default);
#endif
}