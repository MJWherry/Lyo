using Lyo.Exceptions;
using Lyo.Result;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;

namespace Lyo.FileTags.Images;

/// <summary>
/// Reads and writes picture tags through EXIF. Width, height, and format stay on <c>IImageService.GetMetadataAsync</c>.
/// Mapped tags are artist, date, description, and copyright. Any other EXIF name is <see cref="FileTagKey.Custom" />.
/// </summary>
public sealed class ImageFileTagProvider : IFileTagReader, IFileTagWriter
{
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase) {
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".tif", ".tiff"
    };

    /// <inheritdoc />
    public bool CanRead(string path) => HasExtension(path);

    /// <inheritdoc />
    public bool CanWrite(string path) => HasExtension(path);

    /// <inheritdoc />
    public async Task<Result<FileTags>> ReadAsync(string path, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        try {
            using var image = await Image.LoadAsync(path, ct).ConfigureAwait(false);
            return Result<FileTags>.Success(new FileTags(Map(image.Metadata.ExifProfile)));
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            return Result<FileTags>.Failure(ex, "FileTagImageReadFailed");
        }
    }

    /// <inheritdoc />
    public async Task<Result<FileTags>> WriteAsync(string path, FileTagWriteRequest request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(path);
        ArgumentHelpers.ThrowIfNull(request);
        var mapped = Mapped(request.Tags);
        if (!request.ClearExisting && mapped.Count == 0)
            return Result<FileTags>.Failure("No image tags to write.", "FileTagNothingToWrite");

        var full = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(full) ?? ".";
        var temp = Path.Combine(directory, Path.GetFileNameWithoutExtension(full) + ".lyotag" + Path.GetExtension(full));
        try {
            using (var image = await Image.LoadAsync(full, ct).ConfigureAwait(false)) {
                if (request.ClearExisting || image.Metadata.ExifProfile == null)
                    image.Metadata.ExifProfile = new ExifProfile();

                var profile = image.Metadata.ExifProfile;
                if (mapped.TryGetValue(FileTagKey.Artist, out var artist))
                    profile.SetValue(ExifTag.Artist, artist);
                if (mapped.TryGetValue(FileTagKey.Description, out var description))
                    profile.SetValue(ExifTag.ImageDescription, description);
                if (mapped.TryGetValue(FileTagKey.Copyright, out var copyright))
                    profile.SetValue(ExifTag.Copyright, copyright);
                if (mapped.TryGetValue(FileTagKey.Date, out var date))
                    profile.SetValue(ExifTag.DateTimeOriginal, date);

                await image.SaveAsync(temp, ct).ConfigureAwait(false);
            }

            File.Move(temp, full, overwrite: true);
            return Result<FileTags>.Success(new FileTags(mapped));
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            return Result<FileTags>.Failure(ex, "FileTagImageWriteFailed");
        }
        finally {
            if (File.Exists(temp))
                File.Delete(temp);
        }
    }

    private static Dictionary<FileTagKey, string> Map(ExifProfile? profile)
    {
        var tags = new Dictionary<FileTagKey, string>();
        if (profile == null)
            return tags;

        string? date = null;
        var dateRank = 0;
        foreach (var value in profile.Values) {
            var name = value.Tag.ToString();
            if (string.IsNullOrEmpty(name))
                continue;

            string? text;
            try {
                text = value.GetValue()?.ToString();
            }
            catch (Exception) {
                continue;
            }

            if (string.IsNullOrEmpty(text))
                continue;

            var rank = DateRank(name);
            if (rank > 0) {
                if (rank >= dateRank) {
                    date = text;
                    dateRank = rank;
                }

                continue;
            }

            tags[MapName(name)] = text;
        }

        if (date != null)
            tags[FileTagKey.Date] = date;

        return tags;
    }

    private static Dictionary<FileTagKey, string> Mapped(IReadOnlyDictionary<FileTagKey, string> tags)
    {
        var mapped = new Dictionary<FileTagKey, string>();
        foreach (var pair in tags) {
            if (pair.Key == FileTagKey.Artist || pair.Key == FileTagKey.Description || pair.Key == FileTagKey.Copyright || pair.Key == FileTagKey.Date)
                mapped[pair.Key] = pair.Value;
        }

        return mapped;
    }

    private static FileTagKey MapName(string name) => name.ToLowerInvariant() switch {
        "artist" => FileTagKey.Artist,
        "imagedescription" => FileTagKey.Description,
        "copyright" => FileTagKey.Copyright,
        _ => FileTagKey.TryFromId(name) ?? FileTagKey.Custom(name)
    };

    private static int DateRank(string name) => name.ToLowerInvariant() switch {
        "datetimeoriginal" => 3,
        "datetime" => 2,
        "datetimedigitized" => 1,
        _ => 0
    };

    private static bool HasExtension(string path)
        => !string.IsNullOrWhiteSpace(path) && Extensions.Contains(Path.GetExtension(path));
}
