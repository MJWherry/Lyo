using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Components.ParamTable;

/// <summary>
/// Required / Encrypt / Enabled toggles for one parameter row, shared by the table and card layouts of <see cref="LyoParameterEditor" />. Which toggles appear
/// depends on the host: definitions show Required and Encrypt; schedule overrides show Enabled.
/// </summary>
public partial class LyoParameterFlags
{
    /// <summary>Row being edited. Toggles are applied by the host so it can react (clearing ciphertext when Encrypt turns off, for example).</summary>
    [Parameter]
    [EditorRequired]
    public LyoParameterEditRow Item { get; set; } = null!;

    /// <summary>Show the Required toggle for definition parameters.</summary>
    [Parameter]
    public bool ShowRequired { get; set; }

    /// <summary>Show the Encrypt toggle for definition parameters.</summary>
    [Parameter]
    public bool ShowEncrypt { get; set; }

    /// <summary>Show the Enabled toggle for schedule overrides.</summary>
    [Parameter]
    public bool ShowEnabled { get; set; }

    /// <summary>Lay the toggles out in a wrapping row instead of a column. Cards use a row; the table column uses a stack.</summary>
    [Parameter]
    public bool Horizontal { get; set; }

    /// <summary>Fired with the new Required value.</summary>
    [Parameter]
    public EventCallback<bool> RequiredChanged { get; set; }

    /// <summary>Fired with the new Encrypt value. Hosts also clear or seed <see cref="LyoParameterEditRow.EncryptedValue" />.</summary>
    [Parameter]
    public EventCallback<bool> EncryptChanged { get; set; }

    /// <summary>Fired with the new Enabled value.</summary>
    [Parameter]
    public EventCallback<bool> EnabledChanged { get; set; }

    private bool HasAny => ShowRequired || ShowEncrypt || ShowEnabled;

    private Task OnRequiredChanged(bool value) => RequiredChanged.InvokeAsync(value);

    private Task OnEncryptChanged(bool value) => EncryptChanged.InvokeAsync(value);

    private Task OnEnabledChanged(bool value) => EnabledChanged.InvokeAsync(value);
}
