namespace Lyo.Common.Core.Enums;

/// <summary>Buckets used to group related file types.</summary>
public enum FileTypeCategory
{
    /// <summary>Category is not known or not supported</summary>
    Unknown = 0,

    /// <summary>Documents such as PDF, Word, and Excel</summary>
    Documents,

    /// <summary>Images such as JPEG, PNG, and GIF</summary>
    Images,

    /// <summary>Audio such as MP3, WAV, and OGG</summary>
    Audio,

    /// <summary>Archives such as ZIP, RAR, and 7Z</summary>
    Compressed,

    /// <summary>Encrypted files such as GPG and ENC</summary>
    Encrypted,

    /// <summary>Text or data files such as CSV, TXT, JSON, and XML</summary>
    DataFiles,

    /// <summary>Package and distribution artifacts such as NuGet, JVM archives, and OS installers</summary>
    PackageManager,

    /// <summary>Video such as MP4, MKV, AVI, MPEG, MPEG-TS, 3GP, and HLS</summary>
    Video
}