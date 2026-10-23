using Lyo.Web.Components.FileUpload;
using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;

namespace Lyo.Images.Web.Components;

public partial class SpriteSheetAnimateUploader
{
    private LyoFileUpload? _fileUpload;

    [Parameter]
    public IReadOnlyList<AnimatorSheetEntry> Sheets { get; set; } = [];

    [Parameter]
    public Guid? SelectedSheetId { get; set; }

    [Parameter]
    public bool ShowAddImagesHint { get; set; }

    [Parameter]
    public Func<AnimatorSheetEntry, string> GetLabel { get; set; } = static e => e.File.FileName;

    [Parameter]
    public EventCallback<LocalBrowserFile> OnClientFileReady { get; set; }

    [Parameter]
    public EventCallback<LocalBrowserFile> OnClientFileRemoved { get; set; }

    [Parameter]
    public EventCallback<Guid> OnSelectSheet { get; set; }

    [Parameter]
    public EventCallback<AnimatorSheetEntry> OnRemoveSheet { get; set; }

    public Task RemoveClientFileAsync(LocalBrowserFile? file) => _fileUpload?.RemoveClientFileAsync(file) ?? Task.CompletedTask;
}