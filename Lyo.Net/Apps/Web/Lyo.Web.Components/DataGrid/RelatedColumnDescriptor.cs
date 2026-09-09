using Lyo.Api.Client;

namespace Lyo.Web.Components.DataGrid;

/// <summary>
/// One <c>LyoIdColumn</c> registration: parent FK field, related QueryProject route, result CLR type, and Select paths for the related fetch.
/// </summary>
public sealed class RelatedColumnDescriptor
{
    /// <summary>Parent-row field (or property path) that holds the related entity's primary key.</summary>
    public required string Field { get; init; }

    /// <summary>Related entity QueryProject base route, for example <c>PersonAddress</c>.</summary>
    public required string Route { get; init; }

    /// <summary>CLR type of the related projection (<c>TRes</c>). Used to group columns that share a route and deserialize rows.</summary>
    public required Type ResType { get; init; }

    /// <summary>Related QueryProject Select paths, including <c>Id</c>.</summary>
    public required IReadOnlyList<string> Select { get; init; }

    /// <summary>Optional client override; the parent grid's <see cref="IApiClient" /> is used when this is null.</summary>
    public IApiClient? ApiClient { get; init; }

    /// <summary>True when the column is hidden and should not trigger a related fetch.</summary>
    public bool Hidden { get; set; }
}
