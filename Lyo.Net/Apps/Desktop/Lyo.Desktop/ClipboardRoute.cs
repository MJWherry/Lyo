namespace Lyo.Desktop;

internal abstract record ClipboardRoute
{
    internal sealed record Tool(string FileName, string Arguments) : ClipboardRoute;

    internal sealed record Native : ClipboardRoute;
}
