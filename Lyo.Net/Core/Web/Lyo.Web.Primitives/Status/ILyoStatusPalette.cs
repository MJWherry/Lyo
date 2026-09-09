namespace Lyo.Web.Primitives;

/// <summary>
/// Maps one domain's status vocabulary to a chip color, icon, and label so <see cref="LyoStatusChip" /> can render it. Register with <c>AddLyoStatusPalette</c> and
/// name it after the domain (<c>job</c>, <c>report</c>, <c>sms</c>, ...), then request it by name on the chip.
/// </summary>
/// <remarks>
/// A palette replaces the copy-pasted <c>MudChip</c> markup that used to sit beside every <c>*ColorHelper</c>. The helper stays: it keeps the strongly typed
/// <c>ForState(JobState)</c> style API that domain components call directly, and the palette is the thin string-keyed view of it that the shared chip uses.
/// </remarks>
public interface ILyoStatusPalette
{
    /// <summary>Domain name the chip requests through <see cref="LyoStatusChip.Palette" />. Matched case-insensitively.</summary>
    string Name { get; }

    /// <summary>
    /// Appearance for <paramref name="status" />, or null when this palette has no opinion, which lets the chip fall through to the built-in vocabulary.
    /// </summary>
    /// <param name="status">Raw status text as stored or projected, in any casing and with either underscores or hyphens.</param>
    LyoChipSpec? Resolve(string? status);
}
