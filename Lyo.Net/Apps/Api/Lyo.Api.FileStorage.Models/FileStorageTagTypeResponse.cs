namespace Lyo.Api.FileStorage.Models;

/// <summary>One tag type a client can group or sort by.</summary>
/// <param name="TagType">Dimension name.</param>
/// <param name="Automatic">True when the service owns the type. Automatic kinds stay fixed in the structure control.</param>
/// <param name="Sort">Folder order for the default kind: <c>Ordinal</c>, <c>Chronological</c>, <c>Numeric</c>, or <c>Duration</c>.</param>
/// <param name="Grains">Grains the client may request for this type's default kind.</param>
/// <param name="ValueKind">Lyo short name, such as <c>string</c> or <c>datetime</c>.</param>
public sealed record FileStorageTagTypeResponse(string TagType, bool Automatic, string Sort, IReadOnlyList<string> Grains, string ValueKind);
