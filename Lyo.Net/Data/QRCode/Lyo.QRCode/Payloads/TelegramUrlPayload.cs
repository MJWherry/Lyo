using System.Diagnostics;
using System.Text.RegularExpressions;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;

namespace Lyo.QRCode.Payloads;

/// <summary>Opens a Telegram user or channel: <c>https://t.me/&lt;username&gt;</c>.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class TelegramUrlPayload : IQrPayload
{
    private static readonly Regex s_username = new("^[a-zA-Z][a-zA-Z0-9_]{3,31}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Creates a Telegram link (without <c>@</c> prefix).</summary>
    public TelegramUrlPayload(string username)
    {
        ArgumentHelpers.ThrowIfNull(username);
        Username = username.Trim().TrimStart('@');
    }

    /// <summary>Username (no <c>@</c>).</summary>
    public string Username { get; }

    /// <inheritdoc />
    public override string ToString()
        => $"TelegramUrlPayload @{Username}";

    /// <inheritdoc />
    public string ToQrString()
    {
        ArgumentHelpers.ThrowIf(string.IsNullOrWhiteSpace(Username), "Username cannot be empty.", nameof(Username));

        if (!s_username.IsMatch(Username))
            throw new InvalidFormatException("Telegram username must be 4–32 characters, start with a letter, and contain only letters, digits, and underscores.", nameof(Username), Username, "example_channel");

        return "https://t.me/" + Username;
    }
}
