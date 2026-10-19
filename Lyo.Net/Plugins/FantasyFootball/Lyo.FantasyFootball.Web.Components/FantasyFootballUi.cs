using Lyo.Exceptions;

namespace Lyo.FantasyFootball.Web.Components;

/// <summary>Labels for chat topics and trade cards. The workbench and the TypeScript UI use the same wording.</summary>
public static class FantasyFootballUi
{
    /// <summary>League chat, or the other person in a direct message.</summary>
    public static string TopicTitle(FantasyConversationRecord conversation, string? viewerEspnId = null)
    {
        ArgumentHelpers.ThrowIfNull(conversation);
        if (string.Equals(conversation.Type, "CHAT_ALL_MEMBERS", StringComparison.OrdinalIgnoreCase))
            return "League chat";

        var other = conversation.Members.FirstOrDefault(member => !string.Equals(member.EspnMemberId, viewerEspnId, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(other?.DisplayName))
            return other.DisplayName;

        return string.IsNullOrWhiteSpace(other?.EspnMemberId) ? "Direct message" : other.EspnMemberId;
    }

    /// <summary>Sent and received players, using the first item's team as this side.</summary>
    public static string TradeSummary(FantasyTradeRecord trade)
    {
        ArgumentHelpers.ThrowIfNull(trade);
        if (trade.Items.Count == 0)
            return "No players";

        var home = trade.Items[0].FromEspnTeamId;
        var sent = new List<string>();
        var received = new List<string>();
        foreach (var item in trade.Items) {
            var name = string.IsNullOrWhiteSpace(item.PlayerName) ? item.PlayerId?.ToString() ?? "player" : item.PlayerName;
            if (item.FromEspnTeamId == home)
                sent.Add(name);
            else
                received.Add(name);
        }

        return $"Sent {Join(sent)}. Received {Join(received)}.";
    }

    private static string Join(List<string> names) => names.Count == 0 ? "nobody" : string.Join(", ", names);
}
