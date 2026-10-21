using Lyo.Api.Models.Enums;

namespace Lyo.Web.Components.Import.Xlsx;

/// <summary>XLSX choice for the import workbench. A sheet name is supplied on the import request.</summary>
public static class ImportXlsxFormat
{
    /// <summary>Format the XLSX menu item submits.</summary>
    public static ImportFormat Format => ImportFormat.Xlsx;
}