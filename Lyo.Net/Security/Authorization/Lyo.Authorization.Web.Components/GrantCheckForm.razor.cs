using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace Lyo.Authorization.Web.Components;

/// <summary>Asks <see cref="IResourceAuthorization" /> whether a subject may perform an action. A store exception is an error, not a deny.</summary>
public partial class GrantCheckForm
{
    [Inject]
    private IServiceProvider Services { get; set; } = null!;

    private IResourceAuthorization? Authorization => Services.GetService<IResourceAuthorization>();

    private readonly GrantForm _form = new();

    private bool? _allowed;

    private string? _error;

    private bool _busy;

    private async Task CheckAsync()
    {
        var authorization = Authorization;
        if (authorization is null)
            return;

        GrantCheck check;
        try {
            check = _form.ToCheck();
        }
        catch (ArgumentException ex) {
            _error = ex.Message;
            _allowed = null;
            return;
        }

        _busy = true;
        _error = null;
        _allowed = null;
        try {
            _allowed = await authorization.IsAllowedAsync(check);
        }
        catch (Exception ex) {
            _error = ex.Message;
        }
        finally {
            _busy = false;
        }
    }
}
