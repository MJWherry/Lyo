namespace Lyo.Api.FileStorage.Models;

/// <summary>One value kind a structure control can apply to a manual tag.</summary>
/// <param name="Kind">Lyo short name, such as <c>guid</c>, <c>decimal</c>, or <c>timespan</c>.</param>
/// <param name="Sort">Folder order for this kind.</param>
/// <param name="Grains">Grains this kind allows. The grain control is hidden when the only grain is <c>Value</c>.</param>
public sealed record FileStorageTagValueKindResponse(string Kind, string Sort, IReadOnlyList<string> Grains);