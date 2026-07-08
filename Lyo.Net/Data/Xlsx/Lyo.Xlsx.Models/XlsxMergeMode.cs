namespace Lyo.Xlsx.Models;

/// <summary>Strategy for merging multiple XLSX workbooks into one.</summary>
public enum XlsxMergeMode
{
    /// <summary>Copies every worksheet from each input; duplicate sheet names are renamed (e.g. "Sheet1 (2)").</summary>
    PreserveSheets,

    /// <summary>Appends all data rows from every worksheet of every input into a single sheet; the first input's header row wins.</summary>
    ConcatenateRows
}
