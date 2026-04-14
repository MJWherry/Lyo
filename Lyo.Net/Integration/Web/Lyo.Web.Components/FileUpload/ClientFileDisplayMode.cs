namespace Lyo.Web.Components.FileUpload;

/// <summary>How completed client-side files are shown in <see cref="LyoFileUpload" />.</summary>
public enum ClientFileDisplayMode
{
    /// <summary>Per-file chips (and upload summary when <see cref="LyoFileUpload.ShowFiles" /> is false).</summary>
    Chips,

    /// <summary>No chips in this component — handle <see cref="LyoFileUpload.OnClientFileReady" /> in a parent listbox or list.</summary>
    ExternalList
}