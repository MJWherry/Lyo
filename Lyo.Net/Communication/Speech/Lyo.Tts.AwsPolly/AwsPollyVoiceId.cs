using System.ComponentModel;

namespace Lyo.Tts.AwsPolly;

/// <summary>Represents AWS Polly voice IDs.</summary>
public enum AwsPollyVoiceId
{
    /// <summary>Unknown or unspecified voice</summary>
    [Description("Unknown")]
    Unknown = 0,

    // English (US) - Standard
    /// <summary>Joanna (English US, Female, Neural)</summary>
    [Description("Joanna")]
    Joanna = 1,

    /// <summary>Matthew (English US, Male, Neural)</summary>
    [Description("Matthew")]
    Matthew = 2,

    /// <summary>Kendra (English US, Female, Neural)</summary>
    [Description("Kendra")]
    Kendra = 3,

    /// <summary>Kimberly (English US, Female, Neural)</summary>
    [Description("Kimberly")]
    Kimberly = 4,

    /// <summary>Salli (English US, Female, Standard)</summary>
    [Description("Salli")]
    Salli = 5,

    /// <summary>Joey (English US, Male, Standard)</summary>
    [Description("Joey")]
    Joey = 6,

    /// <summary>Justin (English US, Male, Standard)</summary>
    [Description("Justin")]
    Justin = 7,

    /// <summary>Kevin (English US, Male, Neural)</summary>
    [Description("Kevin")]
    Kevin = 8,

    /// <summary>Ivy (English US, Female, Standard)</summary>
    [Description("Ivy")]
    Ivy = 9,

    /// <summary>Amy (English US, Female, Standard)</summary>
    [Description("Amy")]
    Amy = 10,

    /// <summary>Brian (English US, Male, Standard)</summary>
    [Description("Brian")]
    Brian = 11,

    /// <summary>Emma (English US, Female, Standard)</summary>
    [Description("Emma")]
    Emma = 12,

    /// <summary>Nicole (English US, Female, Standard)</summary>
    [Description("Nicole")]
    Nicole = 13,

    /// <summary>Russell (English US, Male, Standard)</summary>
    [Description("Russell")]
    Russell = 14,

    /// <summary>Olivia (English US, Female, Neural)</summary>
    [Description("Olivia")]
    Olivia = 15,

    // English (UK)
    /// <summary>Amy (English UK, Female, Standard)</summary>
    [Description("Amy")]
    AmyUk = 100,

    /// <summary>Emma (English UK, Female, Standard)</summary>
    [Description("Emma")]
    EmmaUk = 101,

    /// <summary>Brian (English UK, Male, Standard)</summary>
    [Description("Brian")]
    BrianUk = 102,

    // English (Australian)
    /// <summary>Nicole (English Australian, Female, Standard)</summary>
    [Description("Nicole")]
    NicoleAu = 200,

    /// <summary>Russell (English Australian, Male, Standard)</summary>
    [Description("Russell")]
    RussellAu = 201,

    /// <summary>Olivia (English Australian, Female, Neural)</summary>
    [Description("Olivia")]
    OliviaAu = 202,

    // Spanish
    /// <summary>Lupe (Spanish US, Female, Neural)</summary>
    [Description("Lupe")]
    Lupe = 300,

    /// <summary>Enrique (Spanish US, Male, Standard)</summary>
    [Description("Enrique")]
    Enrique = 301,

    /// <summary>Conchita (Spanish ES, Female, Standard)</summary>
    [Description("Conchita")]
    Conchita = 302,

    /// <summary>Lucia (Spanish ES, Female, Standard)</summary>
    [Description("Lucia")]
    Lucia = 303,

    /// <summary>Mia (Spanish MX, Female, Neural)</summary>
    [Description("Mia")]
    Mia = 304,

    /// <summary>Penelope (Spanish ES, Female, Standard)</summary>
    [Description("Penelope")]
    Penelope = 305,

    /// <summary>Miguel (Spanish MX, Male, Standard)</summary>
    [Description("Miguel")]
    Miguel = 306,

    // French
    /// <summary>Celine (French FR, Female, Standard)</summary>
    [Description("Celine")]
    Celine = 400,

    /// <summary>Lea (French FR, Female, Standard)</summary>
    [Description("Lea")]
    Lea = 401,

    /// <summary>Mathieu (French FR, Male, Standard)</summary>
    [Description("Mathieu")]
    Mathieu = 402,

    // German
    /// <summary>Marlene (German DE, Female, Standard)</summary>
    [Description("Marlene")]
    Marlene = 500,

    /// <summary>Vicki (German DE, Female, Neural)</summary>
    [Description("Vicki")]
    Vicki = 501,

    /// <summary>Hans (German DE, Male, Standard)</summary>
    [Description("Hans")]
    Hans = 502,

    // Italian
    /// <summary>Carla (Italian IT, Female, Standard)</summary>
    [Description("Carla")]
    Carla = 600,

    /// <summary>Giorgio (Italian IT, Male, Standard)</summary>
    [Description("Giorgio")]
    Giorgio = 601,

    /// <summary>Bianca (Italian IT, Female, Neural)</summary>
    [Description("Bianca")]
    Bianca = 602,

    // Portuguese (Brazil)
    /// <summary>Vitoria (Portuguese BR, Female, Neural)</summary>
    [Description("Vitoria")]
    Vitoria = 700,

    /// <summary>Camila (Portuguese BR, Female, Neural)</summary>
    [Description("Camila")]
    Camila = 701,

    /// <summary>Ricardo (Portuguese BR, Male, Standard)</summary>
    [Description("Ricardo")]
    Ricardo = 702,

    // Japanese
    /// <summary>Mizuki (Japanese JP, Female, Standard)</summary>
    [Description("Mizuki")]
    Mizuki = 800,

    /// <summary>Takumi (Japanese JP, Male, Neural)</summary>
    [Description("Takumi")]
    Takumi = 801,

    // Korean
    /// <summary>Seoyeon (Korean KR, Female, Neural)</summary>
    [Description("Seoyeon")]
    Seoyeon = 900,

    // Chinese
    /// <summary>Zhiyu (Chinese CN, Female, Standard)</summary>
    [Description("Zhiyu")]
    Zhiyu = 1000,

    // Arabic
    /// <summary>Zeina (Arabic, Female, Standard)</summary>
    [Description("Zeina")]
    Zeina = 1100,

    // Hindi
    /// <summary>Aditi (Hindi IN, Female, Standard)</summary>
    [Description("Aditi")]
    Aditi = 1200,

    // Russian
    /// <summary>Tatyana (Russian RU, Female, Standard)</summary>
    [Description("Tatyana")]
    Tatyana = 1300,

    /// <summary>Maxim (Russian RU, Male, Standard)</summary>
    [Description("Maxim")]
    Maxim = 1301,

    // Dutch
    /// <summary>Lotte (Dutch NL, Female, Standard)</summary>
    [Description("Lotte")]
    Lotte = 1400,

    /// <summary>Ruben (Dutch NL, Male, Standard)</summary>
    [Description("Ruben")]
    Ruben = 1401,

    // Polish
    /// <summary>Ewa (Polish PL, Female, Standard)</summary>
    [Description("Ewa")]
    Ewa = 1500,

    /// <summary>Jacek (Polish PL, Male, Standard)</summary>
    [Description("Jacek")]
    Jacek = 1501,

    /// <summary>Jan (Polish PL, Male, Standard)</summary>
    [Description("Jan")]
    Jan = 1502,

    // Turkish
    /// <summary>Filiz (Turkish TR, Female, Standard)</summary>
    [Description("Filiz")]
    Filiz = 1600,

    // Swedish
    /// <summary>Astrid (Swedish SE, Female, Standard)</summary>
    [Description("Astrid")]
    Astrid = 1700,

    // Norwegian
    /// <summary>Ida (Norwegian NO, Female, Standard)</summary>
    [Description("Ida")]
    Ida = 1800,

    // Danish
    /// <summary>Naja (Danish DK, Female, Standard)</summary>
    [Description("Naja")]
    Naja = 1900,

    /// <summary>Mads (Danish DK, Male, Standard)</summary>
    [Description("Mads")]
    Mads = 1901,

    // Finnish
    /// <summary>Suvi (Finnish FI, Female, Standard)</summary>
    [Description("Suvi")]
    Suvi = 2000,

    // Romanian
    /// <summary>Carmen (Romanian RO, Female, Standard)</summary>
    [Description("Carmen")]
    Carmen = 2100,

    // Icelandic
    /// <summary>Dora (Icelandic IS, Female, Standard)</summary>
    [Description("Dora")]
    Dora = 2200,

    // Welsh
    /// <summary>Gwyneth (Welsh GB, Female, Standard)</summary>
    [Description("Gwyneth")]
    Gwyneth = 2300
}