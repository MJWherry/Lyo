using Lyo.Desktop;

namespace Lyo.Screenshot.App;

internal interface IGlobalHotkeys : IDisposable
{
    event Action? StartPressed;

    event Action? StopPressed;

    void Update(HotkeyChord start, HotkeyChord stop);

    void SetEscapeStops(bool enabled);
}
