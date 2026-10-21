using ExcelDataReader;

namespace Lyo.Xlsx;

internal static class ExcelDataTableConfigurationClone
{
    public static ExcelDataTableConfiguration Clone(ExcelDataTableConfiguration source)
        => new() {
            CaseSensitive = source.CaseSensitive,
            EmptyColumnNamePrefix = source.EmptyColumnNamePrefix,
            UseHeaderRow = source.UseHeaderRow,
            ReadHeaderRow = source.ReadHeaderRow,
            ReadHeader = source.ReadHeader,
            FilterRow = source.FilterRow,
            FilterColumn = source.FilterColumn,
            TransformValue = source.TransformValue,
            FillMergedCellsValue = source.FillMergedCellsValue
        };
}