namespace Lyo.PackageMetadata;

/// <summary>Package registry or distribution ecosystem for a <see cref="PackageMetadata" /> row.</summary>
public enum PackageEcosystem
{
    Unknown = 0,
    NuGet,
    Maven,
    Gradle,
    Conan,
    Vcpkg,
    Debian,
    Rpm,
    Msi,
    Other
}