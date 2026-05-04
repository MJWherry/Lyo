using System.Text.Json;
using Lyo.Exceptions;
using Lyo.PackageMetadata;

namespace Lyo.PackageMetadata.Postgres.Database;

internal static class PackageMetadataMapping
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    internal static PackageMetadata ToModel(this PackageMetadataEntity e)
    {
        ArgumentHelpers.ThrowIfNull(e);
        return new(
            e.Id,
            e.Name,
            e.Version,
            e.PackageFileSha512Hex,
            e.Title,
            e.Description,
            DeserializeStringList(e.AuthorsJson),
            DeserializeStringList(e.PackageTypesJson),
            e.ProjectUrl,
            e.RepositoryUrl,
            e.LicenseUrl,
            e.LicenseExpression,
            e.PackageDetailsUrl,
            DeserializeStringList(e.TagsJson),
            e.CreatedAt,
            e.UpdatedAt);
    }

    /// <summary>Copies scalar and JSON list fields from <paramref name="m" /> onto <paramref name="e" /> (not timestamps).</summary>
    internal static void CopyContentFromModel(PackageMetadata m, PackageMetadataEntity e)
    {
        ArgumentHelpers.ThrowIfNull(m);
        ArgumentHelpers.ThrowIfNull(e);
        e.Id = m.Id;
        e.Name = m.Name;
        e.Version = m.Version;
        e.PackageFileSha512Hex = m.PackageFileSha512Hex;
        e.Title = m.Title;
        e.Description = m.Description;
        e.AuthorsJson = SerializeStringList(m.Authors);
        e.PackageTypesJson = SerializeStringList(m.PackageTypes);
        e.TagsJson = SerializeStringList(m.Tags);
        e.ProjectUrl = m.ProjectUrl;
        e.RepositoryUrl = m.RepositoryUrl;
        e.LicenseUrl = m.LicenseUrl;
        e.LicenseExpression = m.LicenseExpression;
        e.PackageDetailsUrl = m.PackageDetailsUrl;
    }

    private static IReadOnlyList<string>? DeserializeStringList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
        }
        catch {
            return null;
        }
    }

    private static string? SerializeStringList(IReadOnlyList<string>? values)
    {
        if (values is null || values.Count == 0)
            return null;
        return JsonSerializer.Serialize(values, JsonOptions);
    }
}
