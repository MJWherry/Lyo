namespace Lyo.FileManager;

/// <summary>Decides whether a press should leave a multi-selection in place.</summary>
public static class FileManagerDragSelection
{
    /// <summary>
    /// True when the pressed row is already one of several selected rows.
    /// A drag then moves the whole selection. A press on an unselected row replaces it.
    /// </summary>
    public static bool Keeps(int selectedCount, bool pressedIsSelected)
        => selectedCount > 1 && pressedIsSelected;
}
