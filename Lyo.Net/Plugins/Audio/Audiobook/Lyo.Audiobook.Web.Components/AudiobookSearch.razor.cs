using Lyo.Web.Primitives.CheckSelect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Lyo.Audiobook.Web.Components;

/// <summary>Audiobook search. The host runs the query and resolves cover URLs. This component does not take a database context.</summary>
public partial class AudiobookSearch
{
    private bool _busy;
    private IReadOnlyList<Audiobook> _results = [];
    private bool _searched;
    private List<string> _selectedTags = [];
    private string? _titleContains;
    private int? _year;

    /// <summary>Host search. The tag list is the current tag filter; the catalog store does not apply tags.</summary>
    [Parameter]
    [EditorRequired]
    public Func<AudiobookQuery, IReadOnlyList<string>, CancellationToken, Task<IReadOnlyList<Audiobook>>>? SearchFunc { get; set; }

    /// <summary>Tag names for <c>Audiobook</c>. The host loads them from the Tag plugin.</summary>
    [Parameter]
    public IReadOnlyList<string> AvailableTags { get; set; } = [];

    /// <summary>Turns <see cref="Audiobook.CoverImageRef" /> into a display URL. Return null for no image.</summary>
    [Parameter]
    public Func<Audiobook, string?>? ResolveCoverUrlFunc { get; set; }

    /// <summary>Fired when the user opens a work.</summary>
    [Parameter]
    public EventCallback<Audiobook> OnWorkOpen { get; set; }

    private IReadOnlyList<LyoSelectOption<string>> TagItems => AvailableTags.Select(t => new LyoSelectOption<string>(t, t)).ToList();

    private string? ResolveCoverUrl(Audiobook work) => ResolveCoverUrlFunc?.Invoke(work);

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
            var query = new AudiobookQuery { TitleContains = _titleContains, Year = _year };
            _results = await SearchFunc(query, _selectedTags, CancellationToken.None);
            _searched = true;
        }
        finally {
            _busy = false;
        }
    }

    private Task OnOpen(Audiobook work) => OnWorkOpen.InvokeAsync(work);
}
