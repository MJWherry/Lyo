using Lyo.Common;
#if !NETSTANDARD2_0
#endif

namespace Lyo.Xlsx.Models;

/// <summary>Imports and parses data from XLSX (Excel) format. Supports parsing to dictionaries and DataTables with optional header row handling.</summary>
public interface IXlsxImporter
{
    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxFileAsDictionary(string xlsxFilePath);

    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxStreamAsDictionary(Stream xlsxStream);

    Result<DataTable.Models.DataTable> ParseXlsxFileAsDataTable(string xlsxFilePath, bool? useHeaderRow = null);

    Result<DataTable.Models.DataTable> ParseXlsxStreamAsDataTable(Stream xlsxStream, bool? useHeaderRow = null);

    Result<DataTable.Models.DataTable> ParseXlsxBytesAsDataTable(byte[] xlsxBytes, bool? useHeaderRow = null);

    IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> ParseXlsxBytesAsDictionary(byte[] xlsxBytes);

#if !NETSTANDARD2_0
    Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxFileAsDictionaryAsync(string xlsxFilePath, CancellationToken ct = default);

    Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxStreamAsDictionaryAsync(Stream xlsxStream, CancellationToken ct = default);

    Task<Result<DataTable.Models.DataTable>> ParseXlsxFileAsDataTableAsync(string xlsxFilePath, bool? useHeaderRow = null, CancellationToken ct = default);

    Task<Result<DataTable.Models.DataTable>> ParseXlsxStreamAsDataTableAsync(Stream xlsxStream, bool? useHeaderRow = null, CancellationToken ct = default);

    Task<Result<DataTable.Models.DataTable>> ParseXlsxBytesAsDataTableAsync(byte[] xlsxBytes, bool? useHeaderRow = null, CancellationToken ct = default);

    Task<IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>>> ParseXlsxBytesAsDictionaryAsync(byte[] xlsxBytes, CancellationToken ct = default);
#endif
}