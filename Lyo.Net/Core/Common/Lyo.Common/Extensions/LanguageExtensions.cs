using Lyo.Common.Records;

namespace Lyo.Common.Extensions;

/// <summary>Extension methods on string tokens for resolving <see cref="LanguageCodeInfo" /> and <see cref="FileTypeInfo" /> from ISO, BCP 47, MIME, and file-extension inputs.</summary>
/// <remarks>The extension receiver is named for ISO 639-1 lookups but is reused as the raw token for each factory method below.</remarks>
public static class LanguageExtensions
{
    extension(string iso6391Code)
    {
        /// <summary>Finds a LanguageCodeInfo by its ISO 639-1 (2-letter) code.</summary>
        /// <returns>The first matching LanguageCodeInfo (base variant), or Unknown if not found.</returns>
        /// <remarks>When multiple language codes share the same ISO 639-1 code, returns the first one (base variant).</remarks>
        public LanguageCodeInfo FromISO639_1() => LanguageCodeInfo.FromIso6391(iso6391Code);

        /// <summary>Finds a LanguageCodeInfo by its ISO 639-3 (3-letter) code.</summary>
        /// <returns>The first matching LanguageCodeInfo (base variant), or Unknown if not found.</returns>
        /// <remarks>When multiple language codes share the same ISO 639-3 code, returns the first one (base variant).</remarks>
        public LanguageCodeInfo FromISO639_3() => LanguageCodeInfo.FromIso6393(iso6391Code);

        /// <summary>Finds a LanguageCodeInfo by its BCP 47 code.</summary>
        /// <returns>The corresponding LanguageCodeInfo, or Unknown if not found.</returns>
        public LanguageCodeInfo FromBCP_47() => LanguageCodeInfo.FromBcp47(iso6391Code);

        /// <summary>Finds a FileTypeInfo by its MIME type value.</summary>
        /// <returns>The corresponding FileTypeInfo, or Unknown if not found.</returns>
        public FileTypeInfo FromMimeValue() => FileTypeInfo.FromMimeType(iso6391Code);

        /// <summary>Finds a FileTypeInfo by its file extension.</summary>
        /// <returns>The corresponding FileTypeInfo, or Unknown if not found.</returns>
        public FileTypeInfo FromExtension() => FileTypeInfo.FromExtension(iso6391Code);
    }
}