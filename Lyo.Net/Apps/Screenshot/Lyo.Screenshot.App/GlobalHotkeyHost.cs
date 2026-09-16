using Lyo.Desktop;

namespace Lyo.Screenshot.App;

internal static class GlobalHotkeyHost
{
    public static IGlobalHotkeys Create() => new ShotHotkeys();
}

/// <summary>Start, stop, and optional Escape on top of <see cref="GlobalHotkeys" />. Escape is best-effort: if it is already grabbed, start and stop stay registered.</summary>
internal sealed class ShotHotkeys : IGlobalHotkeys
{
    private readonly Lyo.Desktop.IGlobalHotkeys _keys = GlobalHotkeys.Create();
    private HotkeyChord _start = HotkeyChord.Parse("Ctrl+Shift+S");
    private HotkeyChord _stop = HotkeyChord.Parse("Ctrl+Shift+X");
    private bool _escape;

    public event Action? StartPressed;

    public event Action? StopPressed;

    public void Update(HotkeyChord start, HotkeyChord stop)
    {
        _start = start;
        _stop = stop;
        Apply();
    }

    public void SetEscapeStops(bool enabled)
    {
        if (_escape == enabled)
            return;
        _escape = enabled;
        Apply();
    }

    public void Dispose() => _keys.Dispose();

    private void Apply()
    {
        var core = new List<HotkeyBinding> {
            new(_start, () => StartPressed?.Invoke()),
            new(_stop, () => StopPressed?.Invoke())
        };
        if (!_escape || IsBareEscape(_stop)) {
            _keys.Bind(core);
            return;
        }

        try {
            var withEscape = new List<HotkeyBinding>(core) { new(HotkeyChord.Parse("Escape"), () => StopPressed?.Invoke()) };
            _keys.Bind(withEscape);
        }
        catch (InvalidOperationException) {
            _keys.Bind(core);
        }
    }

    private static bool IsBareEscape(HotkeyChord chord)
        => chord.Key.Equals("Escape", StringComparison.OrdinalIgnoreCase) && chord.Modifiers == HotkeyModifiers.None;
}
