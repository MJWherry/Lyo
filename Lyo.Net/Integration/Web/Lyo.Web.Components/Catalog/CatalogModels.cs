using System.Text.Json;

namespace Lyo.Web.Components.Catalog;

/// <summary>Docs catalog package entry (mirrors <c>docs/catalog/schema/package.schema.json</c>).</summary>
public sealed class CatalogPackageDoc
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Area { get; set; } = "";
    public string Tagline { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> Features { get; set; } = [];
    public List<CatalogExample> Examples { get; set; } = [];
    public CatalogBenchmarks? Benchmarks { get; set; }
    public List<CatalogSection> Sections { get; set; } = [];
    public List<CatalogLink> Links { get; set; } = [];
    public string ReadmePath { get; set; } = "";
}

/// <summary>Code sample under Examples.</summary>
public sealed class CatalogExample
{
    public string Title { get; set; } = "";
    public string Language { get; set; } = "csharp";
    public string Code { get; set; } = "";
}

/// <summary>Optional benchmarks block.</summary>
public sealed class CatalogBenchmarks
{
    public string? Headline { get; set; }
    public string? Suite { get; set; }
    public List<CatalogBenchmarkItem> Items { get; set; } = [];
}

/// <summary>Benchmark link row.</summary>
public sealed class CatalogBenchmarkItem
{
    public string Label { get; set; } = "";
    public string Href { get; set; } = "";
    public string? Note { get; set; }
}

/// <summary>Related link.</summary>
public sealed class CatalogLink
{
    public string Label { get; set; } = "";
    public string Href { get; set; } = "";
}

/// <summary>Discriminated section (paragraph / list / code / markdown).</summary>
public sealed class CatalogSection
{
    public string Type { get; set; } = "paragraph";
    public string? Title { get; set; }
    public string? Text { get; set; }
    public string? Body { get; set; }
    public string? Code { get; set; }
    public string? Language { get; set; }
    public bool? Ordered { get; set; }
    public List<string>? Items { get; set; }
}

/// <summary>Catalog index row from <c>wwwroot/catalog/index.json</c>.</summary>
public sealed class CatalogIndex
{
    public DateTimeOffset? GeneratedAt { get; set; }
    public int PackageCount { get; set; }
    public List<CatalogIndexPackage> Packages { get; set; } = [];
}

/// <summary>Lightweight package row in the catalog index.</summary>
public sealed class CatalogIndexPackage
{
    public string Id { get; set; } = "";
    public string Area { get; set; } = "";
    public string Name { get; set; } = "";
    public string Tagline { get; set; } = "";
}

/// <summary>Shared JSON options for catalog files.</summary>
public static class CatalogJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };
}
