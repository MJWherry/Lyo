using Lyo.Video.Enums;
using Lyo.Web.Primitives.CheckSelect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Lyo.Video.Web.Components;

/// <summary>Movie and series search. The host runs the query and resolves cover URLs. This component does not take a database context.</summary>
public partial class VideoTitleSearch
{
    private bool _busy;
    private VideoTitleKind? _kind;
    private IReadOnlyList<VideoTitle> _results = [];
    private bool _searched;
    private List<string> _selectedTags = [];
    private string? _titleContains;
    private int? _year;

    /// <summary>Host search. The tag list is the current tag filter; the catalog store does not apply tags.</summary>
    [Parameter]
    [EditorRequired]
    public Func<VideoTitleQuery, IReadOnlyList<string>, CancellationToken, Task<IReadOnlyList<VideoTitle>>>? SearchFunc { get; set; }

    /// <summary>Tag names for <c>VideoTitle</c>. The host loads them from the Tag plugin.</summary>
    [Parameter]
    public IReadOnlyList<string> AvailableTags { get; set; } = [];

    /// <summary>Turns <see cref="VideoTitle.CoverImageRef" /> into a display URL. Return null for no image.</summary>
    [Parameter]
    public Func<VideoTitle, string?>? ResolveCoverUrlFunc { get; set; }

    /// <summary>Fired when the user opens a title.</summary>
    [Parameter]
    public EventCallback<VideoTitle> OnTitleOpen { get; set; }

    private IReadOnlyList<LyoSelectOption<string>> TagItems => AvailableTags.Select(t => new LyoSelectOption<string>(t, t)).ToList();

    private string? ResolveCoverUrl(VideoTitle title) => ResolveCoverUrlFunc?.Invoke(title);

    private async Task OnTitleKeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
            await SearchAsync();
    }

    private async Task SearchAsync()
    {
        if (SearchFunc is null)
            return;

        _busy = true;
        try {
            var query = new VideoTitleQuery { TitleContains = _titleContains, Kind = _kind, Year = _year };
            _results = await SearchFunc(query, _selectedTags, CancellationToken.None);
            _searched = true;
        }
        finally {
            _busy = false;
        }
    }

    private Task OnOpen(VideoTitle title) => OnTitleOpen.InvokeAsync(title);
}