using System.Globalization;
using Lyo.Exceptions;
using Lyo.FileMetadataStore.Models;

namespace Lyo.FileMetadataStore;

/// <summary>Builds one level of a tag-grouped folder from file rows and their tags. Blob keys are not involved.</summary>
public static class FileTagGrouper
{
    /// <summary>
    /// Lists the immediate children of <paramref name="virtualPath" /> for <paramref name="groupBy" />. A file missing the next key, or whose value does not parse, is a file in
    /// the current folder. <c>imported</c> cannot use grain <see cref="FileTagGrain.Value" />.
    /// </summary>
    public static FileTagGroupPage Group(
        IReadOnlyList<FileTagGroupKey> groupBy,
        string? virtualPath,
        IReadOnlyList<FileStoreResult> files,
        IReadOnlyList<FileTag> tags,
        int maxKeys)
    {
        ArgumentHelpers.ThrowIfNull(groupBy);
        ArgumentHelpers.ThrowIfNull(files);
        ArgumentHelpers.ThrowIfNull(tags);
        ArgumentHelpers.ThrowIfLessThan(maxKeys, 1);
        if (groupBy.Count == 0)
            throw new ArgumentException("At least one group key is required.", nameof(groupBy));

        for (var i = 0; i < groupBy.Count; i++)
            ThrowIfGrainIllegal(groupBy[i]);

        var segments = SplitPath(virtualPath);
        if (segments.Count > groupBy.Count)
            throw new ArgumentException("Path is deeper than the group keys.", nameof(virtualPath));

        var tagsByFile = new Dictionary<Guid, List<FileTag>>();
        foreach (var tag in tags) {
            if (!tagsByFile.TryGetValue(tag.FileId, out var list)) {
                list = [];
                tagsByFile[tag.FileId] = list;
            }

            list.Add(tag);
        }

        var matched = new List<FileStoreResult>();
        foreach (var file in files) {
            if (file.DeletedAt != null)
                continue;

            var keep = true;
            for (var i = 0; i < segments.Count; i++) {
                if (!HasSegment(file.Id, groupBy, i, segments[i], tagsByFile)) {
                    keep = false;
                    break;
                }
            }

            if (keep)
                matched.Add(file);
        }

        var parent = segments.Count == 0 ? null : string.Join("/", segments);
        var entries = new List<FileTagGroupEntry>();
        if (segments.Count == groupBy.Count) {
            foreach (var file in matched)
                entries.Add(FileEntry(file, parent));
        }
        else {
            var next = segments.Count;
            var folders = new HashSet<string>(StringComparer.Ordinal);
            foreach (var file in matched) {
                var rendered = RenderAll(file.Id, groupBy, next, tagsByFile);
                if (rendered.Count == 0) {
                    entries.Add(FileEntry(file, parent));
                    continue;
                }

                foreach (var segment in rendered) {
                    if (!folders.Add(segment))
                        continue;

                    var child = parent == null ? segment : parent + "/" + segment;
                    entries.Add(new(segment, true, child, null, null, 0));
                }
            }
        }

        var kind = segments.Count < groupBy.Count ? groupBy[segments.Count].Kind : FileTagValueKind.String;
        entries.Sort((a, b) => CompareEntries(a, b, kind));
        var truncated = entries.Count > maxKeys;
        if (truncated)
            entries = entries.Take(maxKeys).ToList();

        return new(entries, truncated);
    }

    private static void ThrowIfGrainIllegal(FileTagGroupKey key)
    {
        var imported = string.Equals(key.TagType, FileTagTypes.Imported, StringComparison.Ordinal);
        if (key.Grain == FileTagGrain.Value && imported)
            throw new ArgumentException("imported does not group by its raw timestamp.", nameof(key));

        if (!FileTagValueKinds.Allows(key.Kind, key.Grain))
            throw new ArgumentException($"Grain {key.Grain} is not valid for {FileTagValueKinds.Name(key.Kind)}.", nameof(key));
    }

    private static List<string> SplitPath(string? virtualPath)
    {
        if (string.IsNullOrWhiteSpace(virtualPath))
            return [];

        var parts = virtualPath!.Split('/');
        var segments = new List<string>(parts.Length);
        foreach (var part in parts) {
            if (part.Length == 0)
                continue;

            segments.Add(part);
        }

        return segments;
    }

    private static bool HasSegment(Guid fileId, IReadOnlyList<FileTagGroupKey> groupBy, int keyIndex, string segment, Dictionary<Guid, List<FileTag>> tagsByFile)
    {
        foreach (var rendered in RenderAll(fileId, groupBy, keyIndex, tagsByFile)) {
            if (string.Equals(rendered, segment, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static List<string> RenderAll(Guid fileId, IReadOnlyList<FileTagGroupKey> groupBy, int keyIndex, Dictionary<Guid, List<FileTag>> tagsByFile)
    {
        var rendered = new List<string>();
        if (!tagsByFile.TryGetValue(fileId, out var fileTags))
            return rendered;

        var key = groupBy[keyIndex];
        var fixedParts = FixedBefore(groupBy, keyIndex);
        foreach (var tag in fileTags) {
            if (!string.Equals(tag.TagType, key.TagType, StringComparison.Ordinal))
                continue;

            var segment = Render(tag.Value, key, fixedParts);
            if (segment != null)
                rendered.Add(segment);
        }

        return rendered;
    }

    private static FixedParts FixedBefore(IReadOnlyList<FileTagGroupKey> groupBy, int keyIndex)
    {
        var parts = new FixedParts();
        var type = groupBy[keyIndex].TagType;
        for (var i = 0; i < keyIndex; i++) {
            if (string.Equals(groupBy[i].TagType, type, StringComparison.Ordinal))
                parts.Note(groupBy[i].Grain);
        }

        return parts;
    }

    private static string? Render(string value, FileTagGroupKey key, FixedParts fixedParts)
        => key.Kind switch {
            FileTagValueKind.String => key.Grain == FileTagGrain.Value ? value : null,
            FileTagValueKind.Guid => RenderGuid(value),
            FileTagValueKind.Boolean => RenderBoolean(value),
            FileTagValueKind.Integer => RenderInteger(value),
            FileTagValueKind.Number => RenderNumber(value),
            FileTagValueKind.DateTime => RenderDateTime(value, key.Grain, fixedParts),
            FileTagValueKind.TimeOnly => RenderTimeOnly(value, key.Grain, fixedParts),
            FileTagValueKind.TimeSpan => RenderTimeSpan(value, key.Grain, fixedParts),
            var _ => null
        };

    private static string? RenderGuid(string value) => Guid.TryParse(value, out var id) ? id.ToString("D") : null;

    private static string? RenderBoolean(string value) => bool.TryParse(value, out var flag) ? flag ? "true" : "false" : null;

    private static string? RenderInteger(string value)
        => long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ? number.ToString(CultureInfo.InvariantCulture) : null;

    private static string? RenderNumber(string value)
    {
        if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            return number.ToString(CultureInfo.InvariantCulture);

        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var floating) && !double.IsNaN(floating) && !double.IsInfinity(floating))
            return floating.ToString(CultureInfo.InvariantCulture);

        return null;
    }

    private static string? RenderDateTime(string value, FileTagGrain grain, FixedParts fixedParts)
    {
        if (!TryParseUtc(value, out var utc))
            return null;

        return grain switch {
            FileTagGrain.Year => utc.ToString("yyyy", CultureInfo.InvariantCulture),
            FileTagGrain.Month => fixedParts.Year ? utc.ToString("MM", CultureInfo.InvariantCulture) : utc.ToString("yyyy-MM", CultureInfo.InvariantCulture),
            FileTagGrain.Day when fixedParts.Year && fixedParts.Month => utc.ToString("dd", CultureInfo.InvariantCulture),
            FileTagGrain.Day when fixedParts.Year => utc.ToString("MM-dd", CultureInfo.InvariantCulture),
            FileTagGrain.Day => utc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            FileTagGrain.Hour or FileTagGrain.Minute or FileTagGrain.Second => RenderDateClock(utc, grain, fixedParts),
            var _ => null
        };
    }

    private static string RenderDateClock(DateTime utc, FileTagGrain grain, FixedParts fixedParts)
    {
        if (grain == FileTagGrain.Minute && fixedParts.Hour)
            return utc.ToString("mm", CultureInfo.InvariantCulture);

        if (grain == FileTagGrain.Second && fixedParts.Minute)
            return utc.ToString("ss", CultureInfo.InvariantCulture);

        if (grain == FileTagGrain.Second && fixedParts.Hour)
            return utc.ToString("mm:ss", CultureInfo.InvariantCulture);

        var clock = grain switch {
            FileTagGrain.Hour => utc.ToString("HH", CultureInfo.InvariantCulture),
            FileTagGrain.Minute => utc.ToString("HH:mm", CultureInfo.InvariantCulture),
            var _ => utc.ToString("HH:mm:ss", CultureInfo.InvariantCulture)
        };

        if (fixedParts.Day)
            return clock;

        if (fixedParts.Month)
            return utc.ToString("dd", CultureInfo.InvariantCulture) + "T" + clock;

        if (fixedParts.Year)
            return utc.ToString("MM-dd", CultureInfo.InvariantCulture) + "T" + clock;

        return utc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "T" + clock;
    }

    private static string? RenderTimeOnly(string value, FileTagGrain grain, FixedParts fixedParts)
    {
        if (!TryParseTime(value, out var time))
            return null;

        return grain switch {
            FileTagGrain.Hour => Two(time.Hours),
            FileTagGrain.Minute when fixedParts.Hour => Two(time.Minutes),
            FileTagGrain.Minute => Two(time.Hours) + ":" + Two(time.Minutes),
            FileTagGrain.Second when fixedParts.Minute => Two(time.Seconds),
            FileTagGrain.Second when fixedParts.Hour => Two(time.Minutes) + ":" + Two(time.Seconds),
            FileTagGrain.Second => Two(time.Hours) + ":" + Two(time.Minutes) + ":" + Two(time.Seconds),
            var _ => null
        };
    }

    private static string? RenderTimeSpan(string value, FileTagGrain grain, FixedParts fixedParts)
    {
        if (!TryParseDuration(value, out var span))
            return null;

        return grain switch {
            FileTagGrain.Days => Whole(span.TotalDays),
            FileTagGrain.Hours when fixedParts.Days => Two(span.Hours),
            FileTagGrain.Hours => Whole(span.TotalHours),
            FileTagGrain.Minutes when fixedParts.Hours => Two(span.Minutes),
            FileTagGrain.Minutes when fixedParts.Days => Whole(span.Hours * 60 + span.Minutes),
            FileTagGrain.Minutes => Whole(span.TotalMinutes),
            FileTagGrain.Seconds when fixedParts.Minutes => Two(span.Seconds),
            FileTagGrain.Seconds when fixedParts.Hours => Whole(span.Minutes * 60 + span.Seconds),
            FileTagGrain.Seconds when fixedParts.Days => Whole((span.Hours * 60L + span.Minutes) * 60L + span.Seconds),
            FileTagGrain.Seconds => Whole(span.TotalSeconds),
            var _ => null
        };
    }

    private static string Whole(double total) => ((long)total).ToString(CultureInfo.InvariantCulture);

    private static string Two(int value) => value.ToString("00", CultureInfo.InvariantCulture);

    private static bool TryParseUtc(string value, out DateTime utc)
    {
        if (FileTagTypes.TryParseImported(value, out utc))
            return true;

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)) {
            utc = parsed.UtcDateTime;
            return true;
        }

        utc = default;
        return false;
    }

    private static bool TryParseTime(string value, out TimeSpan time)
    {
        if (TryParseDuration(value, out var span) && span >= TimeSpan.Zero && span < TimeSpan.FromDays(1) && value.IndexOf('.') < 0 && value.IndexOf('T') < 0) {
            time = span;
            return true;
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)) {
            time = parsed.TimeOfDay;
            return true;
        }

        time = default;
        return false;
    }

    private static bool TryParseDuration(string value, out TimeSpan span)
    {
#if NET
        return TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out span);
#else
        return TimeSpan.TryParse(value, out span);
#endif
    }

    private static FileTagGroupEntry FileEntry(FileStoreResult file, string? parent)
    {
        var name = string.IsNullOrWhiteSpace(file.OriginalFileName) ? file.Id.ToString("D") : file.OriginalFileName!;
        return new(name, false, parent, file.Id, file.OriginalFileName, file.OriginalFileSize);
    }

    private static int CompareEntries(FileTagGroupEntry a, FileTagGroupEntry b, FileTagValueKind kind)
    {
        if (a.IsDirectory != b.IsDirectory)
            return a.IsDirectory ? -1 : 1;

        if (!a.IsDirectory)
            return StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name);

        return kind switch {
            FileTagValueKind.Guid => StringComparer.Ordinal.Compare(a.Name, b.Name),
            FileTagValueKind.Boolean => CompareBoolean(a.Name, b.Name),
            FileTagValueKind.Integer or FileTagValueKind.Number or FileTagValueKind.TimeSpan => CompareNumeric(a.Name, b.Name),
            FileTagValueKind.DateTime or FileTagValueKind.TimeOnly => StringComparer.Ordinal.Compare(a.Name, b.Name),
            var _ => StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name)
        };
    }

    private static int CompareBoolean(string a, string b)
    {
        var aFalse = string.Equals(a, "false", StringComparison.Ordinal);
        var bFalse = string.Equals(b, "false", StringComparison.Ordinal);
        if (aFalse != bFalse)
            return aFalse ? -1 : 1;

        return StringComparer.Ordinal.Compare(a, b);
    }

    private static int CompareNumeric(string a, string b)
    {
        var aOk = decimal.TryParse(a, NumberStyles.Number, CultureInfo.InvariantCulture, out var aValue);
        var bOk = decimal.TryParse(b, NumberStyles.Number, CultureInfo.InvariantCulture, out var bValue);
        if (aOk && bOk)
            return aValue.CompareTo(bValue);

        return StringComparer.Ordinal.Compare(a, b);
    }

    private struct FixedParts
    {
        public bool Year;
        public bool Month;
        public bool Day;
        public bool Hour;
        public bool Minute;
        public bool Days;
        public bool Hours;
        public bool Minutes;

        public void Note(FileTagGrain grain)
        {
            switch (grain) {
                case FileTagGrain.Year:
                    Year = true;
                    break;
                case FileTagGrain.Month:
                    Month = true;
                    break;
                case FileTagGrain.Day:
                    Day = true;
                    break;
                case FileTagGrain.Hour:
                    Hour = true;
                    break;
                case FileTagGrain.Minute:
                    Minute = true;
                    break;
                case FileTagGrain.Days:
                    Days = true;
                    break;
                case FileTagGrain.Hours:
                    Hours = true;
                    break;
                case FileTagGrain.Minutes:
                    Minutes = true;
                    break;
            }
        }
    }
}