using Lyo.Api.Models.Enums;

namespace Lyo.Web.Components.Import.Csv;

/// <summary>CSV choice for the import workbench.</summary>
public static class ImportCsvFormat
{
    /// <summary>Format the CSV menu item submits.</summary>
    public static ImportFormat Format => ImportFormat.Csv;
}
