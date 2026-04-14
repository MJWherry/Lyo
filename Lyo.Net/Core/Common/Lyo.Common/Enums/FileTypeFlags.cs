using System.ComponentModel;

namespace Lyo.Common.Enums;

/// <summary>Represents supported file types using flags for grouping.</summary>
[Flags]
public enum FileTypeFlags : long
{
    ///// <summary>Unknown or unsupported file type.</summary>
    [Description("Unknown or unsupported file type.")]
    Unknown = 0,

    // 📄 Document Formats

    /// <summary>Adobe PDF document (.pdf).</summary>
    [Description("Adobe PDF document (.pdf).")]
    Pdf = 1 << 0,

    /// <summary>Microsoft Word document (.doc).</summary>
    [Description("Microsoft Word document (.doc).")]
    Doc = 1 << 1,

    /// <summary>Microsoft Word Open XML document (.docx).</summary>
    [Description("Microsoft Word Open XML document (.docx).")]
    Docx = 1 << 2,

    /// <summary>Microsoft Excel spreadsheet (.xls).</summary>
    [Description("Microsoft Excel spreadsheet (.xls).")]
    Xls = 1 << 3,

    /// <summary>Microsoft Excel Open XML spreadsheet (.xlsx).</summary>
    [Description("Microsoft Excel Open XML spreadsheet (.xlsx).")]
    Xlsx = 1 << 4,

    // 🧾 Text & Data

    /// <summary>Comma-separated values file (.csv).</summary>
    [Description("Comma-separated values file (.csv).")]
    Csv = 1 << 5,

    /// <summary>Plain text file (.txt).</summary>
    [Description("Plain text file (.txt).")]
    Txt = 1 << 6,

    /// <summary>LaTeX document file (.tex).</summary>
    [Description("LaTeX document file (.tex).")]
    Tex = 1 << 7,

    // 🌐 Web Formats

    /// <summary>HTML web page (.html).</summary>
    [Description("HTML web page (.html).")]
    Html = 1 << 8,

    /// <summary>HTML web page (.htm).</summary>
    [Description("HTML web page (.htm).")]
    Htm = 1 << 9,

    /// <summary>JSON data file (.json).</summary>
    [Description("JSON data file (.json).")]
    Json = 1 << 10,

    /// <summary>XML data file (.xml).</summary>
    [Description("XML data file (.xml).")]
    Xml = 1 << 11,

    // 🧱 Binary & Dump Files

    /// <summary>Raw binary file (.bin).</summary>
    [Description("Raw binary file (.bin).")]
    Bin = 1 << 12,

    /// <summary>Generic data dump file (.dump).</summary>
    [Description("Generic data dump file (.dump).")]
    Dump = 1 << 13,

    // 🖼️ Image Formats

    /// <summary>JPEG image file (.jpg).</summary>
    [Description("JPEG image file (.jpg).")]
    Jpg = 1 << 14,

    /// <summary>JPEG image file (.jpeg).</summary>
    [Description("JPEG image file (.jpeg).")]
    Jpeg = 1 << 15,

    /// <summary>Portable Network Graphics image (.png).</summary>
    [Description("Portable Network Graphics image (.png).")]
    Png = 1 << 16,

    /// <summary>Graphics Interchange Format image (.gif).</summary>
    [Description("Graphics Interchange Format image (.gif).")]
    Gif = 1 << 17,

    /// <summary>Bitmap image file (.bmp).</summary>
    [Description("Bitmap image file (.bmp).")]
    Bmp = 1 << 18,

    /// <summary>Scalable Vector Graphics image (.svg).</summary>
    [Description("Scalable Vector Graphics image (.svg).")]
    Svg = 1 << 19,

    /// <summary>Tagged Image File Format (.tif).</summary>
    [Description("Tagged Image File Format (.tif).")]
    Tif = 1 << 20,

    /// <summary>Tagged Image File Format (.tiff).</summary>
    [Description("Tagged Image File Format (.tiff).")]
    Tiff = 1 << 21,

    /// <summary>WebP image file (.webp).</summary>
    [Description("WebP image file (.webp).")]
    Webp = 1 << 22,

    // 📦 Compressed Formats

    /// <summary>ZIP archive file (.zip).</summary>
    [Description("ZIP archive file (.zip).")]
    Zip = 1 << 23,

    /// <summary>RAR archive file (.rar).</summary>
    [Description("RAR archive file (.rar).")]
    Rar = 1 << 24,

    /// <summary>7-Zip archive file (.7z).</summary>
    [Description("7-Zip archive file (.7z).")]
    SevenZip = 1 << 25,

    /// <summary>TAR archive file (.tar).</summary>
    [Description("TAR archive file (.tar).")]
    Tar = 1 << 26,

    /// <summary>GZIP compressed file (.gz).</summary>
    [Description("GZIP compressed file (.gz).")]
    Gz = 1 << 27,

    /// <summary>BZIP2 compressed file (.bz2).</summary>
    [Description("BZIP2 compressed file (.bz2).")]
    Bz2 = 1 << 28,

    /// <summary>XZ compressed file (.xz).</summary>
    [Description("XZ compressed file (.xz).")]
    Xz = 1 << 29,

    // 🔒 Encrypted Formats

    /// <summary>Encrypted file (.enc).</summary>
    [Description("Encrypted file (.enc).")]
    Enc = 1 << 30,

    /// <summary>GPG encrypted file (.gpg).</summary>
    [Description("GPG encrypted file (.gpg).")]
    Gpg = 1L << 31,

    // 🎵 Audio Formats

    /// <summary>WAV audio file (.wav).</summary>
    [Description("WAV audio file (.wav).")]
    Wav = 1L << 32,

    /// <summary>MP3 audio file (.mp3).</summary>
    [Description("MP3 audio file (.mp3).")]
    Mp3 = 1L << 33,

    /// <summary>OGG audio file (.ogg).</summary>
    [Description("OGG audio file (.ogg).")]
    Ogg = 1L << 34,

    /// <summary>FLAC audio file (.flac).</summary>
    [Description("FLAC audio file (.flac).")]
    Flac = 1L << 35,

    /// <summary>AAC audio file (.aac).</summary>
    [Description("AAC audio file (.aac).")]
    Aac = 1L << 36,

    /// <summary>M4A audio file (.m4a).</summary>
    [Description("M4A audio file (.m4a).")]
    M4a = 1L << 37,

    /// <summary>OPUS audio file (.opus).</summary>
    [Description("OPUS audio file (.opus).")]
    Opus = 1L << 38,

    /// <summary>PCM audio file (.pcm).</summary>
    [Description("PCM audio file (.pcm).")]
    Pcm = 1L << 39,

    /// <summary>WebM audio file (.webm).</summary>
    [Description("WebM audio file (.webm).")]
    Webm = 1L << 40,

    // Category flags
    Images = Jpg | Jpeg | Png | Gif | Bmp | Svg | Tif | Tiff | Webp,
    Documents = Pdf | Doc | Docx | Xls | Xlsx,
    DataFiles = Csv | Txt | Tex | Html | Htm | Json | Xml | Bin | Dump,
    Compressed = Zip | Rar | SevenZip | Tar | Gz | Bz2 | Xz,
    Encrypted = Enc | Gpg,
    Audio = Wav | Mp3 | Ogg | Flac | Aac | M4a | Opus | Pcm | Webm,

    All = Pdf | Doc | Docx | Xls | Xlsx | Csv | Txt | Tex | Html | Htm | Json | Xml | Bin | Dump | Jpg | Jpeg | Png | Gif | Bmp | Svg | Tif | Tiff | Webp | Zip | Rar | SevenZip |
        Tar | Gz | Bz2 | Xz | Enc | Gpg | Wav | Mp3 | Ogg | Flac | Aac | M4a | Opus | Pcm | Webm
}