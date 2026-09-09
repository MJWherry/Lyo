using System.ComponentModel;

namespace Lyo.Common.Core.Enums;

/// <summary>File-type bits that can be combined for grouping.</summary>
[Flags]
public enum FileTypeFlags : long
{
    ///// <summary>File type is not known or not supported.</summary>
    [Description("Unknown or unsupported file type.")]
    Unknown = 0,

    // Document formats

    /// <summary>Adobe PDF (.pdf).</summary>
    [Description("Adobe PDF document (.pdf).")]
    Pdf = 1 << 0,

    /// <summary>Microsoft Word (.doc).</summary>
    [Description("Microsoft Word document (.doc).")]
    Doc = 1 << 1,

    /// <summary>Microsoft Word Open XML (.docx).</summary>
    [Description("Microsoft Word Open XML document (.docx).")]
    Docx = 1 << 2,

    /// <summary>Microsoft Excel (.xls).</summary>
    [Description("Microsoft Excel spreadsheet (.xls).")]
    Xls = 1 << 3,

    /// <summary>Microsoft Excel Open XML (.xlsx).</summary>
    [Description("Microsoft Excel Open XML spreadsheet (.xlsx).")]
    Xlsx = 1 << 4,

    // Text and data

    /// <summary>Comma-separated values (.csv).</summary>
    [Description("Comma-separated values file (.csv).")]
    Csv = 1 << 5,

    /// <summary>Plain text (.txt).</summary>
    [Description("Plain text file (.txt).")]
    Txt = 1 << 6,

    /// <summary>LaTeX (.tex).</summary>
    [Description("LaTeX document file (.tex).")]
    Tex = 1 << 7,

    // Web formats

    /// <summary>HTML page (.html).</summary>
    [Description("HTML web page (.html).")]
    Html = 1 << 8,

    /// <summary>HTML page (.htm).</summary>
    [Description("HTML web page (.htm).")]
    Htm = 1 << 9,

    /// <summary>JSON (.json).</summary>
    [Description("JSON data file (.json).")]
    Json = 1 << 10,

    /// <summary>XML (.xml).</summary>
    [Description("XML data file (.xml).")]
    Xml = 1 << 11,

    // Binary and dump files

    /// <summary>Raw binary (.bin).</summary>
    [Description("Raw binary file (.bin).")]
    Bin = 1 << 12,

    /// <summary>Generic dump (.dump).</summary>
    [Description("Generic data dump file (.dump).")]
    Dump = 1 << 13,

    // Image formats

    /// <summary>JPEG (.jpg).</summary>
    [Description("JPEG image file (.jpg).")]
    Jpg = 1 << 14,

    /// <summary>JPEG (.jpeg).</summary>
    [Description("JPEG image file (.jpeg).")]
    Jpeg = 1 << 15,

    /// <summary>Portable Network Graphics (.png).</summary>
    [Description("Portable Network Graphics image (.png).")]
    Png = 1 << 16,

    /// <summary>Graphics Interchange Format (.gif).</summary>
    [Description("Graphics Interchange Format image (.gif).")]
    Gif = 1 << 17,

    /// <summary>Bitmap (.bmp).</summary>
    [Description("Bitmap image file (.bmp).")]
    Bmp = 1 << 18,

    /// <summary>Scalable Vector Graphics (.svg).</summary>
    [Description("Scalable Vector Graphics image (.svg).")]
    Svg = 1 << 19,

    /// <summary>Tagged Image File Format image (.tif).</summary>
    [Description("Tagged Image File Format (.tif).")]
    Tif = 1 << 20,

    /// <summary>Tagged Image File Format image (.tiff).</summary>
    [Description("Tagged Image File Format (.tiff).")]
    Tiff = 1 << 21,

    /// <summary>WebP (.webp).</summary>
    [Description("WebP image file (.webp).")]
    Webp = 1 << 22,

    // Compressed formats

    /// <summary>ZIP archive (.zip).</summary>
    [Description("ZIP archive file (.zip).")]
    Zip = 1 << 23,

    /// <summary>RAR archive (.rar).</summary>
    [Description("RAR archive file (.rar).")]
    Rar = 1 << 24,

    /// <summary>7-Zip archive (.7z).</summary>
    [Description("7-Zip archive file (.7z).")]
    SevenZip = 1 << 25,

    /// <summary>TAR archive (.tar).</summary>
    [Description("TAR archive file (.tar).")]
    Tar = 1 << 26,

    /// <summary>GZIP (.gz).</summary>
    [Description("GZIP compressed file (.gz).")]
    Gz = 1 << 27,

    /// <summary>BZIP2 (.bz2).</summary>
    [Description("BZIP2 compressed file (.bz2).")]
    Bz2 = 1 << 28,

    /// <summary>XZ (.xz).</summary>
    [Description("XZ compressed file (.xz).")]
    Xz = 1 << 29,

    // Encrypted formats

    /// <summary>Encrypted payload (.enc).</summary>
    [Description("Encrypted file (.enc).")]
    Enc = 1 << 30,

    /// <summary>GPG-encrypted payload (.gpg).</summary>
    [Description("GPG encrypted file (.gpg).")]
    Gpg = 1L << 31,

    // Audio formats

    /// <summary>WAV audio (.wav).</summary>
    [Description("WAV audio file (.wav).")]
    Wav = 1L << 32,

    /// <summary>MP3 audio (.mp3).</summary>
    [Description("MP3 audio file (.mp3).")]
    Mp3 = 1L << 33,

    /// <summary>OGG audio (.ogg).</summary>
    [Description("OGG audio file (.ogg).")]
    Ogg = 1L << 34,

    /// <summary>FLAC audio (.flac).</summary>
    [Description("FLAC audio file (.flac).")]
    Flac = 1L << 35,

    /// <summary>AAC audio (.aac).</summary>
    [Description("AAC audio file (.aac).")]
    Aac = 1L << 36,

    /// <summary>M4A audio (.m4a).</summary>
    [Description("M4A audio file (.m4a).")]
    M4a = 1L << 37,

    /// <summary>OPUS audio (.opus).</summary>
    [Description("OPUS audio file (.opus).")]
    Opus = 1L << 38,

    /// <summary>PCM audio (.pcm).</summary>
    [Description("PCM audio file (.pcm).")]
    Pcm = 1L << 39,

    /// <summary>WebM audio (.webm).</summary>
    [Description("WebM audio file (.webm).")]
    Webm = 1L << 40,

    // Package and distribution

    /// <summary>NuGet package (.nupkg).</summary>
    [Description("NuGet package archive (.nupkg).")]
    Nupkg = 1L << 41,

    /// <summary>NuGet symbols package (.snupkg).</summary>
    [Description("NuGet symbol package archive (.snupkg).")]
    Snupkg = 1L << 42,

    /// <summary>Java archive JAR (.jar).</summary>
    [Description("Java archive (JAR) (.jar).")]
    Jar = 1L << 43,

    /// <summary>Java web application archive WAR (.war).</summary>
    [Description("Java web application archive (WAR) (.war).")]
    War = 1L << 44,

    /// <summary>Java enterprise archive EAR (.ear).</summary>
    [Description("Java enterprise archive (EAR) (.ear).")]
    Ear = 1L << 45,

    /// <summary>Android library archive AAR (.aar).</summary>
    [Description("Android library archive (AAR) (.aar).")]
    Aar = 1L << 46,

    /// <summary>Debian package (.deb).</summary>
    [Description("Debian binary package (.deb).")]
    Deb = 1L << 47,

    /// <summary>RPM package file (.rpm).</summary>
    [Description("RPM package (.rpm).")]
    Rpm = 1L << 48,

    /// <summary>Windows Installer MSI (.msi).</summary>
    [Description("Windows Installer package (MSI) (.msi).")]
    Msi = 1L << 49,

    /// <summary>JavaScript source (.js).</summary>
    [Description("JavaScript source file (.js).")]
    Js = 1L << 50,

    /// <summary>GraphQL source (.graphql).</summary>
    [Description("GraphQL document (.graphql).")]
    Graphql = 1L << 51,

    /// <summary>GraphQL source (.gql).</summary>
    [Description("GraphQL document (.gql).")]
    Gql = 1L << 52,

    /// <summary>URL-encoded HTTP form placeholder file (.form).</summary>
    [Description("URL-encoded HTTP form (.form).")]
    UrlEncodedForm = 1L << 53,

    // Video formats

    /// <summary>MPEG-4 video (.mp4).</summary>
    [Description("MPEG-4 video file (.mp4).")]
    Mp4 = 1L << 54,

    /// <summary>QuickTime movie (.mov).</summary>
    [Description("QuickTime movie file (.mov).")]
    Mov = 1L << 55,

    /// <summary>Matroska video (.mkv).</summary>
    [Description("Matroska video file (.mkv).")]
    Mkv = 1L << 56,

    /// <summary>Audio Video Interleave (.avi).</summary>
    [Description("Audio Video Interleave file (.avi).")]
    Avi = 1L << 57,

    /// <summary>MPEG video (.mpeg).</summary>
    [Description("MPEG video file (.mpeg).")]
    Mpeg = 1L << 58,

    /// <summary>MPEG transport stream (.ts).</summary>
    [Description("MPEG transport stream (.ts).")]
    Ts = 1L << 59,

    /// <summary>3GPP mobile video (.3gp).</summary>
    [Description("3GPP mobile video file (.3gp).")]
    ThreeGp = 1L << 60,

    /// <summary>HTTP Live Streaming playlist (.m3u8).</summary>
    [Description("HTTP Live Streaming playlist (.m3u8).")]
    M3u8 = 1L << 61,

    // Grouping bits
    Images = Jpg | Jpeg | Png | Gif | Bmp | Svg | Tif | Tiff | Webp,
    Documents = Pdf | Doc | Docx | Xls | Xlsx,
    DataFiles = Csv | Txt | Tex | Html | Htm | Json | Xml | Bin | Dump | Js | Graphql | Gql | UrlEncodedForm,
    Compressed = Zip | Rar | SevenZip | Tar | Gz | Bz2 | Xz,
    Encrypted = Enc | Gpg,
    Audio = Wav | Mp3 | Ogg | Flac | Aac | M4a | Opus | Pcm | Webm,
    PackageManager = Nupkg | Snupkg | Jar | War | Ear | Aar | Deb | Rpm | Msi,
    Video = Mp4 | Mov | Mkv | Avi | Mpeg | Ts | ThreeGp | M3u8,

    All = Pdf | Doc | Docx | Xls | Xlsx | Csv | Txt | Tex | Html | Htm | Json | Xml | Bin | Dump | Js | Graphql | Gql | UrlEncodedForm | Jpg | Jpeg | Png | Gif | Bmp | Svg | Tif |
        Tiff | Webp | Zip | Rar | SevenZip | Tar | Gz | Bz2 | Xz | Enc | Gpg | Wav | Mp3 | Ogg | Flac | Aac | M4a | Opus | Pcm | Webm | Nupkg | Snupkg | Jar | War | Ear | Aar |
        Deb | Rpm | Msi | Mp4 | Mov | Mkv | Avi | Mpeg | Ts | ThreeGp | M3u8
}