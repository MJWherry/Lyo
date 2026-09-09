namespace Lyo.PackageMetadata;

/// <summary>Binds one or more namespace prefixes to a <see cref="PackageMetadata" /> (longest registered prefix wins at lookup).</summary>
public sealed record PackageMetadataRegistration(IReadOnlyList<string> NamespacePrefixes, PackageMetadata Package);