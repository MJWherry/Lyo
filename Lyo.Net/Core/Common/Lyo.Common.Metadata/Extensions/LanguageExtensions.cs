using Lyo.Common.Metadata.Records;

namespace Lyo.Common.Metadata.Extensions;

/// <summary>Helpers on string tokens that resolve <see cref="LanguageCodeInfo" /> and <see cref="FileTypeInfo" /> from ISO, BCP 47, MIME, and extension inputs.</summary>
/// <remarks>The receiver is named for ISO 639-1 lookups but is treated as the raw token by every factory below.</remarks>
public static class LanguageExtensions
{
    extension(string iso6391Code)
    {
        /// <summary>Looks up <see cref="LanguageCodeInfo" /> by ISO 639-1 (two-letter) code.</summary>
        /// <returns>The first matching row (base variant), or <see cref="LanguageCodeInfo.Unknown" /> when none match.</returns>
        /// <remarks>When several catalog rows share an ISO 639-1 code, the first registered (base) variant wins.</remarks>
        public LanguageCodeInfo FromISO639_1() => LanguageCodeInfo.FromIso6391(iso6391Code);

        /// <summary>Looks up <see cref="LanguageCodeInfo" /> by ISO 639-3 (three-letter) code.</summary>
        /// <returns>The first matching row (base variant), or <see cref="LanguageCodeInfo.Unknown" /> when none match.</returns>
        /// <remarks>When several catalog rows share an ISO 639-3 code, the first registered (base) variant wins.</remarks>
        public LanguageCodeInfo FromISO639_3() => LanguageCodeInfo.FromIso6393(iso6391Code);

        /// <summary>Looks up <see cref="LanguageCodeInfo" /> by BCP 47 tag.</summary>
        /// <returns>The matching row, or <see cref="LanguageCodeInfo.Unknown" /> when the tag is not registered.</returns>
        public LanguageCodeInfo FromBCP_47() => LanguageCodeInfo.FromBcp47(iso6391Code);

        /// <summary>Looks up <see cref="FileTypeInfo" /> by MIME string.</summary>
        /// <returns>The matching row, or <see cref="FileTypeInfo.Unknown" /> when the MIME is not registered.</returns>
        public FileTypeInfo FromMimeValue() => FileTypeInfo.FromMimeType(iso6391Code);

        /// <summary>Looks up <see cref="FileTypeInfo" /> by file extension.</summary>
        /// <returns>The matching row, or <see cref="FileTypeInfo.Unknown" /> when the extension is not registered.</returns>
        public FileTypeInfo FromExtension() => FileTypeInfo.FromExtension(iso6391Code);
    }
}