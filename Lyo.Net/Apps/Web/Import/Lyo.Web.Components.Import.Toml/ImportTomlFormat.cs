using Lyo.Api.Models.Enums;

namespace Lyo.Web.Components.Import.Toml;

/// <summary>TOML choice for the import workbench.</summary>
public static class ImportTomlFormat
{
    /// <summary>Format the TOML menu item submits.</summary>
    public static ImportFormat Format => ImportFormat.Toml;
}