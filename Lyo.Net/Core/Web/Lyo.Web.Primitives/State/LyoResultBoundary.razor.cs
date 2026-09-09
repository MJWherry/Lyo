using System.Collections;
using Lyo.Result;
using Lyo.Result.Interfaces;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Primitives;

/// <summary>
/// Renders the four states a loaded panel can be in — loading, failed, empty, and populated — from one <see cref="IResult{T}" />, so components stop hand-writing the
/// same <c>if (loading) … else if (error) …</c> ladder around each fetch.
/// </summary>
/// <remarks>
/// Pass the result straight from the store or API client and put the success markup in <c>ChildContent</c>, which receives the non-null data. Each branch has an
/// override for the cases where the defaults do not fit.
/// <code>
/// &lt;LyoResultBoundary T="IReadOnlyList&lt;Schedule&gt;" Result="@_result" Loading="@_reloading" OnRetry="LoadAsync"
///                    EmptyTitle="No schedules yet"&gt;
///     &lt;ChildContent Context="schedules"&gt;
///         @foreach (var schedule in schedules) { &lt;ScheduleRow Value="@schedule"/&gt; }
///     &lt;/ChildContent&gt;
/// &lt;/LyoResultBoundary&gt;
/// </code>
/// A null <see cref="Result" /> means "not fetched yet" and shows the loading branch, so the first render does not flash an empty panel.
/// </remarks>
/// <typeparam name="T">Type carried by the result on success.</typeparam>
public partial class LyoResultBoundary<T>
{
    /// <summary>The result to render. Null means the fetch has not completed, which shows the loading branch.</summary>
    [Parameter]
    public IResult<T>? Result { get; set; }

    /// <summary>Forces the loading branch while a refresh runs over data that is already on screen.</summary>
    [Parameter]
    public bool Loading { get; set; }

    /// <summary>Success markup. Receives the result data, already checked for null.</summary>
    [Parameter]
    [EditorRequired]
    public RenderFragment<T>? ChildContent { get; set; }

    /// <summary>Replaces the default <see cref="LyoSkeletonPanel" />.</summary>
    [Parameter]
    public RenderFragment? LoadingContent { get; set; }

    /// <summary>Replaces the default <see cref="LyoEmptyState" />.</summary>
    [Parameter]
    public RenderFragment? EmptyContent { get; set; }

    /// <summary>Replaces the default error alert. Receives the result errors, or an empty list when the result failed without any.</summary>
    [Parameter]
    public RenderFragment<IReadOnlyList<Error>>? ErrorContent { get; set; }

    /// <summary>
    /// Decides whether successful data counts as empty. Defaults to null data and, for collections, zero items — so a custom predicate is only needed for a wrapper
    /// type such as a paged envelope.
    /// </summary>
    [Parameter]
    public Func<T, bool>? IsEmpty { get; set; }

    /// <summary>Headline used by the default empty state.</summary>
    [Parameter]
    public string EmptyTitle { get; set; } = "Nothing to show";

    /// <summary>Second line used by the default empty state.</summary>
    [Parameter]
    public string? EmptyDescription { get; set; }

    /// <summary>Icon used by the default empty state.</summary>
    [Parameter]
    public string? EmptyIcon { get; set; } = Icons.Material.Filled.Inbox;

    /// <summary>Actions for the default empty state, such as clearing the filter that hid every row.</summary>
    [Parameter]
    public RenderFragment? EmptyActions { get; set; }

    /// <summary>Row count used by the default skeleton.</summary>
    [Parameter]
    public int SkeletonRows { get; set; } = 4;

    /// <summary>Wire this up to show a Retry button on the default error alert. Omit it and no button is drawn.</summary>
    [Parameter]
    public EventCallback OnRetry { get; set; }

    /// <summary>Branch currently being rendered. Exposed so a parent can mirror the state in a toolbar or header.</summary>
    public LyoResultPhase Phase
        => Loading || Result is null ? LyoResultPhase.Loading
            : !Result.IsSuccess ? LyoResultPhase.Error
            : ChildContent is null || Result.Data is null || IsDataEmpty(Result.Data) ? LyoResultPhase.Empty
            : LyoResultPhase.Content;

    private IReadOnlyList<Error> Errors => Result?.Errors ?? [];

    private string ErrorText => LyoResultErrorFormatter.FormatErrors(Result?.Errors);

    private bool IsDataEmpty(T data)
        => IsEmpty is not null
            ? IsEmpty(data)
            : data is string text ? text.Length == 0 : data is ICollection collection && collection.Count == 0;
}
