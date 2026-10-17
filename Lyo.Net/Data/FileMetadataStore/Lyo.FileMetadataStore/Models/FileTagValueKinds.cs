namespace Lyo.FileMetadataStore.Models;

/// <summary>Short names, legal grains, and sort for <see cref="FileTagValueKind" />. Query tokens use these names.</summary>
public static class FileTagValueKinds
{
    private static readonly FileTagGrain[] ValueOnly = [FileTagGrain.Value];
    private static readonly FileTagGrain[] DateGrains = [FileTagGrain.Year, FileTagGrain.Month, FileTagGrain.Day, FileTagGrain.Hour, FileTagGrain.Minute, FileTagGrain.Second];
    private static readonly FileTagGrain[] TimeGrains = [FileTagGrain.Hour, FileTagGrain.Minute, FileTagGrain.Second];
    private static readonly FileTagGrain[] SpanGrains = [FileTagGrain.Days, FileTagGrain.Hours, FileTagGrain.Minutes, FileTagGrain.Seconds];

    /// <summary>Kinds a structure control can offer, in display order.</summary>
    public static IReadOnlyList<FileTagValueKind> All { get; } =
    [
        FileTagValueKind.String,
        FileTagValueKind.Guid,
        FileTagValueKind.Boolean,
        FileTagValueKind.Integer,
        FileTagValueKind.Number,
        FileTagValueKind.DateTime,
        FileTagValueKind.TimeOnly,
        FileTagValueKind.TimeSpan
    ];

    /// <summary>Query name for <paramref name="kind" />. <c>datetimeoffset</c> is accepted by <see cref="TryParse" /> and reported as <c>datetime</c>.</summary>
    /// <param name="kind">Value kind.</param>
    /// <returns>Lyo short name, such as <c>int</c>, <c>decimal</c>, or <c>timeonly</c>.</returns>
    public static string Name(FileTagValueKind kind)
        => kind switch {
            FileTagValueKind.Guid => "guid",
            FileTagValueKind.Boolean => "bool",
            FileTagValueKind.Integer => "int",
            FileTagValueKind.Number => "decimal",
            FileTagValueKind.DateTime => "datetime",
            FileTagValueKind.TimeOnly => "timeonly",
            FileTagValueKind.TimeSpan => "timespan",
            _ => "string"
        };

    /// <summary>
    /// Parses a Lyo short name. <c>long</c> is an integer. <c>double</c> is a number. <c>datetimeoffset</c> is a UTC date-time. <c>time</c> is a time of day.
    /// </summary>
    /// <param name="text">Kind token.</param>
    /// <param name="kind">Parsed kind when this returns true.</param>
    /// <returns>True when <paramref name="text" /> names a kind.</returns>
    public static bool TryParse(string text, out FileTagValueKind kind)
    {
        switch (text.Trim().ToLowerInvariant()) {
            case "string":
                kind = FileTagValueKind.String;
                return true;
            case "guid":
            case "uuid":
                kind = FileTagValueKind.Guid;
                return true;
            case "bool":
            case "boolean":
                kind = FileTagValueKind.Boolean;
                return true;
            case "int":
            case "long":
            case "integer":
                kind = FileTagValueKind.Integer;
                return true;
            case "decimal":
            case "double":
            case "number":
                kind = FileTagValueKind.Number;
                return true;
            case "datetime":
            case "datetimeoffset":
                kind = FileTagValueKind.DateTime;
                return true;
            case "timeonly":
            case "time":
                kind = FileTagValueKind.TimeOnly;
                return true;
            case "timespan":
            case "duration":
                kind = FileTagValueKind.TimeSpan;
                return true;
            default:
                kind = default;
                return false;
        }
    }

    /// <summary>
    /// Kind implied when a query writes a grain and no kind. Time-of-day grains are a time of day. <see cref="FileTagGrain.Value" /> is a string.
    /// </summary>
    /// <param name="grain">Grain token.</param>
    /// <returns>The kind that grain belongs to when the query omits a kind.</returns>
    public static FileTagValueKind KindForGrain(FileTagGrain grain)
        => grain switch {
            FileTagGrain.Year or FileTagGrain.Month or FileTagGrain.Day => FileTagValueKind.DateTime,
            FileTagGrain.Hour or FileTagGrain.Minute or FileTagGrain.Second => FileTagValueKind.TimeOnly,
            FileTagGrain.Days or FileTagGrain.Hours or FileTagGrain.Minutes or FileTagGrain.Seconds => FileTagValueKind.TimeSpan,
            _ => FileTagValueKind.String
        };

    /// <summary>Grains a grouping row of <paramref name="kind" /> may use. String, guid, bool, integer, and number are <see cref="FileTagGrain.Value" /> only.</summary>
    /// <param name="kind">Value kind.</param>
    /// <returns>Legal grains.</returns>
    public static IReadOnlyList<FileTagGrain> GrainsFor(FileTagValueKind kind)
        => kind switch {
            FileTagValueKind.DateTime => DateGrains,
            FileTagValueKind.TimeOnly => TimeGrains,
            FileTagValueKind.TimeSpan => SpanGrains,
            _ => ValueOnly
        };

    /// <summary>True when <paramref name="grain" /> is a folder component of <paramref name="kind" />.</summary>
    /// <param name="kind">Value kind.</param>
    /// <param name="grain">Requested grain.</param>
    /// <returns>True when the pair is legal.</returns>
    public static bool Allows(FileTagValueKind kind, FileTagGrain grain)
    {
        foreach (var allowed in GrainsFor(kind)) {
            if (allowed == grain)
                return true;
        }

        return false;
    }

    /// <summary>Folder order for <paramref name="kind" />. Date and time of day are chronological. Durations sort by length. Integers and numbers sort numerically.</summary>
    /// <param name="kind">Value kind.</param>
    /// <returns>Sort the capabilities response should report.</returns>
    public static FileTagSort SortFor(FileTagValueKind kind)
        => kind switch {
            FileTagValueKind.DateTime or FileTagValueKind.TimeOnly => FileTagSort.Chronological,
            FileTagValueKind.TimeSpan => FileTagSort.Duration,
            FileTagValueKind.Integer or FileTagValueKind.Number => FileTagSort.Numeric,
            _ => FileTagSort.Ordinal
        };
}
