namespace Lyo.Webhook.Twilio;

/// <summary>URL variants Twilio may use when computing <c>X-Twilio-Signature</c> (default port present or omitted).</summary>
public static class TwilioUrlNormalization
{
    /// <summary>When the URI uses the scheme default port, returns the same URL with an explicit <c>:443</c> or <c>:80</c> segment.</summary>
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

    /// <summary>When the URI includes a non-default port, returns an equivalent URL using the scheme default port.</summary>
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