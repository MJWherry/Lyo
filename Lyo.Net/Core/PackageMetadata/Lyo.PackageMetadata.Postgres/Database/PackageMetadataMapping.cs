using System.Text.Json;
using Lyo.Exceptions;

namespace Lyo.PackageMetadata.Postgres.Database;

internal static class PackageMetadataMapping
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    internal static PackageMetadata ToModel(this PackageMetadataEntity e)
    {
        ArgumentHelpers.ThrowIfNull(e);
        return new(
            e.Id, e.Ecosystem, e.Name, e.Version, e.ArtifactDigestAlgorithm, e.ArtifactDigestHex, e.Title, e.Description, DeserializeStringList(e.AuthorsJson),
            DeserializeStringList(e.PackageTypesJson), e.ProjectUrl, e.RepositoryUrl, e.LicenseUrl, e.LicenseExpression, ResolveLicenseExpressionSyntax(e), e.PackageDetailsUrl,
            DeserializeStringList(e.TagsJson), e.CreatedAt, e.UpdatedAt);
    }

    /// <summary>Copies scalar and JSON list fields from <paramref name="m" /> onto <paramref name="e" /> (not timestamps).</summary>
    internal static void CopyContentFromModel(PackageMetadata m, PackageMetadataEntity e)
    {
        ArgumentHelpers.ThrowIfNull(m);
        ArgumentHelpers.ThrowIfNull(e);
        e.Id = m.Id;
        e.Ecosystem = m.Ecosystem;
        e.Name = m.Name;
        e.Version = m.Version;
        e.ArtifactDigestAlgorithm = m.ArtifactDigestAlgorithm;
        e.ArtifactDigestHex = m.ArtifactDigestHex;
        e.Title = m.Title;
        e.Description = m.Description;
        e.AuthorsJson = SerializeStringList(m.Authors);
        e.PackageTypesJson = SerializeStringList(m.PackageTypes);
        e.TagsJson = SerializeStringList(m.Tags);
        e.ProjectUrl = m.ProjectUrl;
        e.RepositoryUrl = m.RepositoryUrl;
        e.LicenseUrl = m.LicenseUrl;
        e.LicenseExpression = m.LicenseExpression;
        e.LicenseExpressionSyntaxJson = SerializeSyntax(PackageLicenseExpression.TryParseSyntax(m.LicenseExpression));
        e.PackageDetailsUrl = m.PackageDetailsUrl;
    }

    private static SpdxLicenseExpressionSyntax? ResolveLicenseExpressionSyntax(PackageMetadataEntity e)
    {
        var fromJson = DeserializeSyntax(e.LicenseExpressionSyntaxJson);
        if (fromJson is not null)
            return fromJson;

        return PackageLicenseExpression.TryParseSyntax(e.LicenseExpression);
    }

    private static SpdxLicenseExpressionSyntax? DeserializeSyntax(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try {
            return JsonSerializer.Deserialize<SpdxLicenseExpressionSyntax>(json, JsonOptions);
        }
        catch {
            return null;
        }
    }

    private static string? SerializeSyntax(SpdxLicenseExpressionSyntax? syntax)
    {
        if (syntax is null)
            return null;

        return JsonSerializer.Serialize(syntax, JsonOptions);
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