namespace Lyo.Api.Import;

/// <summary>Caps for one import. Crossing either cap fails the document and returns no rows.</summary>
public sealed class ImportOptions
{
    /// <summary>Configuration section. Bind is a no-op when the section is absent, so these defaults remain.</summary>
    public const string SectionName = "Import";

    /// <summary>Maximum UTF-8 bytes of <see cref="Lyo.Api.Models.Common.Request.ImportRequest.Content" />, or workbook length. Starts at 20 MiB.</summary>
    public int MaxImportBytes { get; set; } = 20 * 1024 * 1024;

    /// <summary>Maximum source rows (good rows plus row issues). Starts at 100000 and is not tied to export page size.</summary>
    public int MaxImportRows { get; set; } = 100_000;

    /// <summary>Throws when a cap is not positive.</summary>
    public void Validate()
    {
        if (MaxImportBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaxImportBytes));

        if (MaxImportRows <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaxImportRows));
    }
}