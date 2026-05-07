using Lyo.Result;

namespace Lyo.Xlsx.Models;

/// <summary>Imports XLSX workbooks: first-sheet dictionary or Lyo data table, synchronous and asynchronous.</summary>
public interface IXlsxImporter
{
    /// <summary>Parses the first worksheet from a file into row → column → cell text.</summary>
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxFileAsDictionary(string xlsxFilePath);

    /// <summary>Parses the first worksheet from a stream into row → column → cell text.</summary>
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxStreamAsDictionary(Stream xlsxStream);

    /// <summary>Parses the first worksheet into a Lyo data table.</summary>
    Result<DataTable.Models.DataTable> ParseXlsxFileAsDataTable(string xlsxFilePath, bool? useHeaderRow = null);

    /// <summary>Parses the first worksheet from a stream into a Lyo data table.</summary>
    Result<DataTable.Models.DataTable> ParseXlsxStreamAsDataTable(Stream xlsxStream, bool? useHeaderRow = null);

    /// <summary>Parses the first worksheet from bytes into a Lyo data table.</summary>
    Result<DataTable.Models.DataTable> ParseXlsxBytesAsDataTable(byte[] xlsxBytes, bool? useHeaderRow = null);

    /// <summary>Parses the first worksheet from bytes into a nested dictionary.</summary>
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxBytesAsDictionary(byte[] xlsxBytes);

#if !NETSTANDARD2_0
    /// <summary>Asynchronously parses the first worksheet from a file into a nested dictionary.</summary>
    Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxFileAsDictionaryAsync(string xlsxFilePath, CancellationToken ct = default);

    /// <summary>Asynchronously parses the first worksheet from a stream into a nested dictionary.</summary>
    Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxStreamAsDictionaryAsync(Stream xlsxStream, CancellationToken ct = default);

    /// <summary>Asynchronously parses the first worksheet into a Lyo data table.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseXlsxFileAsDataTableAsync(string xlsxFilePath, bool? useHeaderRow = null, CancellationToken ct = default);

    /// <summary>Asynchronously parses the first worksheet from a stream into a Lyo data table.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseXlsxStreamAsDataTableAsync(Stream xlsxStream, bool? useHeaderRow = null, CancellationToken ct = default);

    /// <summary>Asynchronously parses the first worksheet from bytes into a Lyo data table.</summary>
    Task<Result<DataTable.Models.DataTable>> ParseXlsxBytesAsDataTableAsync(byte[] xlsxBytes, bool? useHeaderRow = null, CancellationToken ct = default);

    /// <summary>Asynchronously parses the first worksheet from bytes into a nested dictionary.</summary>
    Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxBytesAsDictionaryAsync(byte[] xlsxBytes, CancellationToken ct = default);
#endif
}