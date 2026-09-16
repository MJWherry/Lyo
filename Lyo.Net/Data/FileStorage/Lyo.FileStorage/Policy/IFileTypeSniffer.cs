using Lyo.Common.Metadata.Records;

namespace Lyo.FileStorage.Policy;

/// <summary>Identifies a file type from a leading byte prefix.</summary>
public interface IFileTypeSniffer
{
    /// <summary>Identifies a file from a header prefix.</summary>
    /// <param name="header">Leading bytes. Thirty-two bytes is enough for every signature this package checks.</param>
    /// <returns><see cref="FileTypeInfo.Unknown" /> when the prefix matches no signature.</returns>
    FileTypeInfo Identify(ReadOnlySpan<byte> header);
}
