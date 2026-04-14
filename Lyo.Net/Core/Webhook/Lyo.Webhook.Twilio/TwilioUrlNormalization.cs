namespace Lyo.Webhook.Twilio;

/// <summary>URL variants Twilio may use when computing <c>X-Twilio-Signature</c> (with or without explicit default port).</summary>
public static class TwilioUrlNormalization
{
    /// <summary>If the URI uses the default port for the scheme, returns the same URL with an explicit <c>:443</c> or <c>:80</c> segment.</summary>
    public static string AddExplicitDefaultPort(string url)
    {
        try {
            var u = new Uri(url);
            if (!u.IsDefaultPort)
                return url;

            var b = new UriBuilder(u) { Port = u.Scheme == Uri.UriSchemeHttps ? 443 : 80 };
            return b.Uri.ToString();
        }
        catch (UriFormatException) {
            return url;
        }
    }

    /// <summary>If the URI includes a non-default port, returns an equivalent URL with the default port for the scheme.</summary>
    public static string RemoveNonDefaultPort(string url)
    {
        try {
            var u = new Uri(url);
            if (u.IsDefaultPort)
                return url;

            var b = new UriBuilder(u) { Port = -1 };
            return b.Uri.ToString();
        }
        catch (UriFormatException) {
            return url;
        }
    }
}