namespace Lyo.PackageMetadata;

/// <summary>Associates one or more namespace prefixes with a <see cref="PackageMetadata" /> (longest registered prefix wins at lookup).</summary>
public sealed record PackageMetadataRegistration(IReadOnlyList<string> NamespacePrefixes, PackageMetadata Package);