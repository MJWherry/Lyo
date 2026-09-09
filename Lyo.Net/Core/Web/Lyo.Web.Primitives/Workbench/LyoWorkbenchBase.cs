using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Primitives;

/// <summary>
/// Base class for the interactive <c>*Workbench</c> components (the try-it panels for TTS, barcodes, RabbitMQ, locks, and so on). It owns the transient status banner those panels
/// all show after an operation: the message is mirrored to a snackbar and clears itself after <see cref="StatusAutoClearDelay" /> unless a newer message arrives sooner.
/// </summary>
/// <remarks>
/// Inherit and render the banner with <c>LyoWorkbenchStatus</c>:
/// <code>
/// @inherits LyoWorkbenchBase
///
/// &lt;LyoWorkbenchStatus Message="@StatusMessage" Severity="@StatusSeverity"/&gt;
/// &lt;MudButton OnClick="RunAsync"&gt;Run&lt;/MudButton&gt;
///
/// @code {
///     private async Task RunAsync()
///     {
///         try {
///             await Service.DoWorkAsync();
///             SetStatus("Done.", Severity.Success);
///         }
///         catch (Exception ex) {
///             SetStatus(ex.Message, Severity.Error);
///         }
///     }
/// }
/// </code>
/// Do not declare <c>@inject ISnackbar Snackbar</c> on a derived component; that hides the inherited <see cref="Snackbar" /> and gives you two instances.
/// </remarks>
public abstract class LyoWorkbenchBase : ComponentBase
{
    private int _statusVersion;

    /// <summary>Snackbar used to mirror every <see cref="SetStatus" /> message, so the outcome stays visible even when the panel is scrolled out of view.</summary>
    [Inject]
    protected ISnackbar Snackbar { get; set; } = null!;

    /// <summary>Current status text, or empty when there is nothing to show. Bind this to <c>LyoWorkbenchStatus.Message</c>.</summary>
    protected string StatusMessage { get; private set; } = string.Empty;

    /// <summary>Severity of the current status text.</summary>
    protected Severity StatusSeverity { get; private set; } = Severity.Info;

    /// <summary>How long a status message stays on screen. Override for panels whose operations warrant a longer read.</summary>
    protected virtual TimeSpan StatusAutoClearDelay => TimeSpan.FromMilliseconds(2500);

    /// <summary>
    /// Shows <paramref name="message" /> in the panel banner and the snackbar, and schedules it to clear. Safe to call from any handler; the clear is versioned, so a later message
    /// always beats an earlier one's expiry.
    /// </summary>
    /// <param name="message">Text to display.</param>
    /// <param name="severity">Severity driving the banner and snackbar colour.</param>
    protected void SetStatus(string message, Severity severity)
    {
        StatusMessage = message;
        StatusSeverity = severity;
        var version = ++_statusVersion;
        Snackbar.Add(message, severity);
        _ = ClearStatusLaterAsync(version);
    }

    /// <summary>Hides the banner immediately. Use when resetting a panel, for example after clearing its inputs and results.</summary>
    protected void ClearStatus()
    {
        StatusMessage = string.Empty;
        _statusVersion++;
    }

    private async Task ClearStatusLaterAsync(int version)
    {
        await Task.Delay(StatusAutoClearDelay);
        if (version != _statusVersion)
            return;

        StatusMessage = string.Empty;
        await InvokeAsync(StateHasChanged);
    }
}
