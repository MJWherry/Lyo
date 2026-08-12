using System.Diagnostics;
using System.Globalization;
using System.Text;
using Lyo.DataTable.Models;

namespace Lyo.Csv.Models;

/// <summary>
/// Service-level options for CSV dialect, encoding, and DataTable value pooling. CSV defaults disable value pooling (unique-heavy grids); enable via <see cref="Pooling" />
/// or config when duplication is high.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class CsvOptions
{
    /// <summary>Configuration section name for binding.</summary>
    public const string SectionName = "Csv";

    /// <summary>Field delimiter (single character as string). Default <c>,</c>.</summary>
    public string Delimiter { get; set; } = ",";

    /// <summary>When true, the first row is treated as column headers for typed mapping and DataTable imports (unless overridden).</summary>
    public bool HasHeaderRecord { get; set; } = true;

    /// <summary>When true, blank lines are skipped while reading.</summary>
    public bool IgnoreBlankLines { get; set; } = true;

    /// <summary>When true, field values are trimmed of leading/trailing whitespace after parse.</summary>
    public bool TrimFields { get; set; } = true;

    /// <summary>When true, rows whose field count differs from the first data/header row throw <see cref="CsvBadDataException" />.</summary>
    public bool DetectColumnCountChanges { get; set; } = true;

    /// <summary>Culture used for typed value conversion.</summary>
    public CultureInfo Culture { get; set; } = CultureInfo.InvariantCulture;

    /// <summary>Text encoding for file/stream read and write. Default UTF-8.</summary>
    public Encoding Encoding { get; set; } = new UTF8Encoding(false);

    /// <summary>Quote character wrapping fields that contain delimiter, quote, escape, or newlines. Default <c>"</c>.</summary>
    public char Quote { get; set; } = '"';

    /// <summary>
    /// Escape character. When equal to <see cref="Quote" />, doubled-quote RFC style is used (<c>""</c>). When different (e.g. <c>\</c>), escape-prefix style is used inside
    /// quoted fields.
    /// </summary>
    public char Escape { get; set; } = '"';

    /// <summary>When true, lines whose first non-whitespace character is <see cref="Comment" /> are skipped (outside quoted fields).</summary>
    public bool AllowComments { get; set; }

    /// <summary>Comment line marker when <see cref="AllowComments" /> is enabled. Default <c>#</c>.</summary>
    public char Comment { get; set; } = '#';

    /// <summary>Pooling options for CSV → DataTable imports. Defaults: <see cref="DataTablePoolingOptions.PoolValues" /> = false (format pooling unused for CSV).</summary>
    public DataTablePoolingOptions Pooling { get; set; } = CreateDefaultPooling();

    /// <summary>Creates CSV-oriented pooling defaults (<c>PoolValues=false</c>, <c>PoolFormats=false</c>).</summary>
    public static DataTablePoolingOptions CreateDefaultPooling() => new() { PoolValues = false, PoolFormats = false };

    /// <summary>Validates dialect and nested options.</summary>
    public void Validate()
    {
        if (string.IsNullOrEmpty(Delimiter) || Delimiter.Length != 1)
            throw new ArgumentException("Delimiter must be a single character.", nameof(Delimiter));

        var delimiter = Delimiter[0];
        if (Quote == delimiter)
            throw new ArgumentException("Quote must differ from Delimiter.", nameof(Quote));

        if (AllowComments && Comment == delimiter)
            throw new ArgumentException("Comment must differ from Delimiter when AllowComments is enabled.", nameof(Comment));

        if (Culture is null)
            throw new ArgumentNullException(nameof(Culture));

        if (Encoding is null)
            throw new ArgumentNullException(nameof(Encoding));

        Pooling.Validate();
    }

    /// <summary>Returns a shallow copy suitable for mutation (e.g. <see cref="Encoding" /> via <c>SetEncoding</c>).</summary>
    public CsvOptions Clone()
        => new() {
            Delimiter = Delimiter,
            HasHeaderRecord = HasHeaderRecord,
            IgnoreBlankLines = IgnoreBlankLines,
            TrimFields = TrimFields,
            DetectColumnCountChanges = DetectColumnCountChanges,
            Culture = Culture,
            Encoding = Encoding,
            Quote = Quote,
            Escape = Escape,
            AllowComments = AllowComments,
            Comment = Comment,
            Pooling = Pooling
        };

    /// <inheritdoc />
    public override string ToString()
        => $"CsvOptions: Delimiter={Delimiter}, Quote={Quote}, Escape={Escape}, HasHeader={HasHeaderRecord}, AllowComments={AllowComments}, Pooling=({Pooling})";
}