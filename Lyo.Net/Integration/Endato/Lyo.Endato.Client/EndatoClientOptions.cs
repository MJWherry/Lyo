namespace Lyo.Endato.Client;

public class EndatoClientOptions
{
    public const string SectionName = "EndatoClient";

    public string Url { get; set; } = null!;

    public string ApName { get; set; } = null!;

    public string ApPassword { get; set; } = null!;

    /// <summary>Gets or sets whether to ensure HTTP success status codes (default true). When false, responses are returned regardless of status code.</summary>
    public bool EnsureStatusCode { get; set; } = true;
}