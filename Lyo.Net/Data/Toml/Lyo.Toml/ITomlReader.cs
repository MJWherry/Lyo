using System.Text;
using Lyo.Result;

namespace Lyo.Toml;

/// <summary>Reads a TOML document into a typed value from a file, stream, byte array, or string.</summary>
public interface ITomlReader
{
    /// <summary>Reads <paramref name="tomlFilePath" /> as <typeparamref name="T" />.</summary>
    T ParseFile<T>(string tomlFilePath);

    /// <summary>Reads <paramref name="tomlStream" /> as <typeparamref name="T" />. The stream is left open.</summary>
    T ParseStream<T>(Stream tomlStream);

    /// <summary>Reads <paramref name="tomlBytes" /> as <typeparamref name="T" />. A null <paramref name="encoding" /> uses the service encoding.</summary>
    T ParseBytes<T>(byte[] tomlBytes, Encoding? encoding = null);

    /// <summary>Reads <paramref name="toml" /> as <typeparamref name="T" />. A null <paramref name="encoding" /> uses the service encoding.</summary>
    T Parse<T>(ReadOnlySpan<byte> toml, Encoding? encoding = null);

    /// <summary>Reads already-decoded <paramref name="toml" /> as <typeparamref name="T" />.</summary>
    T Parse<T>(ReadOnlySpan<char> toml);

    /// <summary>Reads <paramref name="toml" /> as <typeparamref name="T" />. A root array of tables named by <see cref="TomlOptions.ItemsKey" /> binds <c>List&lt;T&gt;</c>.</summary>
    T ParseString<T>(string toml);

    /// <summary>
    /// Reads the items array of tables into a data table. <paramref name="hasFooterRow" /> moves the last table into the footer. A document without that array, or a nested cell,
    /// fails the result. Cell text stays text.
    /// </summary>
    Result<DataTable.Models.DataTable> ParseStringAsDataTable(string toml, bool hasFooterRow = false);

    /// <summary>Reads <paramref name="tomlBytes" /> as a data table. A null <paramref name="encoding" /> uses the service encoding.</summary>
    Result<DataTable.Models.DataTable> ParseBytesAsDataTable(byte[] tomlBytes, bool hasFooterRow = false, Encoding? encoding = null);

    /// <summary>Reads <paramref name="tomlStream" /> as a data table. The stream is left open.</summary>
    Result<DataTable.Models.DataTable> ParseStreamAsDataTable(Stream tomlStream, bool hasFooterRow = false);

    /// <summary>Reads <paramref name="tomlFilePath" /> as a data table.</summary>
    Result<DataTable.Models.DataTable> ParseFileAsDataTable(string tomlFilePath, bool hasFooterRow = false);

#if !NETSTANDARD2_0
    /// <summary>Reads <paramref name="tomlFilePath" /> as <typeparamref name="T" />.</summary>
    Task<T> ParseFileAsync<T>(string tomlFilePath, CancellationToken ct = default);

    /// <summary>Reads <paramref name="tomlStream" /> as <typeparamref name="T" />. The stream is left open.</summary>
    Task<T> ParseStreamAsync<T>(Stream tomlStream, CancellationToken ct = default);

    /// <summary>Reads UTF-8 <paramref name="tomlBytes" /> as <typeparamref name="T" />.</summary>
    Task<T> ParseBytesAsync<T>(byte[] tomlBytes, CancellationToken ct = default);

    /// <summary>Reads <paramref name="toml" /> as <typeparamref name="T" />.</summary>
    Task<T> ParseStringAsync<T>(string toml, CancellationToken ct = default);

    /// <summary>Reads <paramref name="toml" /> as a data table.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseStringAsDataTableAsync(string toml, bool hasFooterRow = false, CancellationToken ct = default);

    /// <summary>Reads <paramref name="tomlBytes" /> as a data table. A null <paramref name="encoding" /> uses the service encoding.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseBytesAsDataTableAsync(byte[] tomlBytes, bool hasFooterRow = false, Encoding? encoding = null, CancellationToken ct = default);

    /// <summary>Reads <paramref name="tomlStream" /> as a data table. The stream is left open.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseStreamAsDataTableAsync(Stream tomlStream, bool hasFooterRow = false, CancellationToken ct = default);

    /// <summary>Reads <paramref name="tomlFilePath" /> as a data table.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseFileAsDataTableAsync(string tomlFilePath, bool hasFooterRow = false, CancellationToken ct = default);
#endif
}
