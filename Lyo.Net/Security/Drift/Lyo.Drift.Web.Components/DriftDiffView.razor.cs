using System.Text.Json;
using Lyo.Common.Json;

namespace Lyo.Drift.Web.Components;

public partial class DriftDiffView
{
    /// <summary>Stored diff to display.</summary>
    [Parameter]
    [EditorRequired]
    public DriftDiffRes Diff { get; set; } = null!;

    private List<FileSystemChangeDto> _fileChanges = [];
    private List<ObjectGraphDifferenceDto> _systemDiffs = [];

    protected override void OnParametersSet()
    {
        _fileChanges = DeserializeList<FileSystemChangeDto>(Diff.FileChangesJson);
        _systemDiffs = DeserializeList<ObjectGraphDifferenceDto>(Diff.SystemDifferencesJson);
    }

    private static List<T> DeserializeList<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        return JsonSerializer.Deserialize<List<T>>(json, LyoJsonSerializerOptions.Create()) ?? [];
    }
}
