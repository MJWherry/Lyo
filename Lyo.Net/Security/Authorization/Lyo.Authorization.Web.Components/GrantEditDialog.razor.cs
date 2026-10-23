using Lyo.Exceptions.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lyo.Authorization.Web.Components;

/// <summary>Creates one grant. A conflict leaves the dialog open.</summary>
public partial class GrantEditDialog
{
    private readonly GrantForm _form = new();

    private bool _busy;

    private bool _lockSubject;

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = null!;

    /// <summary>Store used when <see cref="Authorization" /> is not registered.</summary>
    [Parameter]
    [EditorRequired]
    public IGrantStore Store { get; set; } = null!;

    /// <summary>Audited save path. Falls back to <see cref="Store" /> when null.</summary>
    [Parameter]
    public IResourceAuthorization? Authorization { get; set; }

    /// <summary>When set, the subject field is fixed to this value.</summary>
    [Parameter]
    public string? Subject { get; set; }

    /// <summary>Actor stored on the grant.</summary>
    [Parameter]
    public string? GrantedBy { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!string.IsNullOrWhiteSpace(Subject)) {
            _form.Subject = Subject;
            _lockSubject = true;
        }

        _form.GrantedBy = GrantedBy;
    }

    private void Cancel() => MudDialog.Cancel();

    private async Task SaveAsync()
    {
        GrantWrite write;
        try {
            write = _form.ToWrite();
        }
        catch (ArgumentException ex) {
            Snackbar.Add(ex.Message, Severity.Warning);
            return;
        }

        _busy = true;
        try {
            if (Authorization is not null)
                await Authorization.SaveAsync(write);
            else
                await Store.SaveAsync(write);

            MudDialog.Close(DialogResult.Ok(true));
        }
        catch (ConflictException ex) {
            Snackbar.Add(ex.Message, Severity.Warning);
        }
        catch (ArgumentException ex) {
            Snackbar.Add(ex.Message, Severity.Warning);
        }
        finally {
            _busy = false;
        }
    }
}