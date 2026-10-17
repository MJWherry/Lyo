using System.Globalization;
using System.Text.Json;
using Lyo.Espn.Fantasy.Football.Client.Models.Response;

namespace Lyo.Espn.Fantasy.Football.Client;

/// <summary>Copies ESPN chat JSON into the public chat models and fills members and teams from the league index.</summary>
internal static class ChatMaps
{
    internal static ChatConversationRes Conversation(EspnChatConversation wire, LeagueIndex? index)
    {
        IReadOnlyList<ChatMessageRes>? messages = wire.Messages?.Select(message => Message(message, index)).ToArray();
        var conversation = new ChatConversationRes(
            wire.Id ?? "",
            wire.Author,
            wire.Date,
            wire.DateEdited,
            wire.IsDeleted,
            wire.IsEdited,
            messages,
            wire.TotalMessageCount,
            wire.Type,
            wire.UnreadMessageCount,
            wire.Subject,
            wire.ViewableBy);
        conversation.Author = Member(index, wire.Author);
        conversation.Members = Members(index, conversation.ViewableByIds);
        return conversation;
    }

    internal static ChatMessageRes Message(EspnChatMessage wire, LeagueIndex? index)
    {
        var attributes = ReadAttributes(wire.Attributes);
        var slots = ReadSlots(attributes);
        var tradeKeys = slots.Exists(slot => slot.TradeKey) || HasKey(attributes, "tId");
        var addDropKeys = slots.Exists(slot => !slot.TradeKey);
        var kind = KindOf(wire.MessageTypeId, tradeKeys, addDropKeys);
        ChatMessageRes message;
        if (kind is ChatMessageKind.Text or ChatMessageKind.Unknown)
            message = new(wire.Id ?? "", wire.Author, wire.Date, wire.IsDeleted, wire.TopicId, kind, wire.Content, IsEdited: wire.IsEdited);
        else
            message = new(
                wire.Id ?? "",
                wire.Author,
                wire.Date,
                wire.IsDeleted,
                wire.TopicId,
                kind,
                wire.Content,
                TransactionId(attributes),
                Side(1, attributes, slots),
                Side(2, attributes, slots),
                wire.IsEdited);

        message.Author = Member(index, wire.Author);
        AssignTeam(message.Side1, index);
        AssignTeam(message.Side2, index);
        return message;
    }

    private static MemberRes? Member(LeagueIndex? index, string? authorId)
        => LeagueIndex.IsLeagueManager(authorId) ? null : index?.Member(authorId);

    private static void AssignTeam(ChatMessageSide? side, LeagueIndex? index)
    {
        if (side?.TeamId is int id)
            side.Team = index?.Team(id);
    }

    private static List<MemberRes> Members(LeagueIndex? index, IReadOnlyList<string> ids)
    {
        var members = new List<MemberRes>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in ids) {
            if (string.IsNullOrWhiteSpace(id) || !seen.Add(id))
                continue;

            var member = index?.Member(id);
            if (member != null)
                members.Add(member);
        }

        return members;
    }

    private static ChatMessageKind KindOf(int typeId, bool tradeKeys, bool addDropKeys)
    {
        if (typeId == 271 || tradeKeys)
            return ChatMessageKind.Trade;
        if (typeId == 266 || addDropKeys)
            return ChatMessageKind.AddDrop;
        if (typeId is 0 or 270)
            return ChatMessageKind.Text;

        return ChatMessageKind.Unknown;
    }

    private static ChatMessageSide? Side(int number, IReadOnlyDictionary<string, string>? attributes, List<PlayerSlot> slots)
    {
        int? teamId = TryInt(attributes, number == 1 ? "team1" : "team2", out var id) ? id : null;
        var added = Players(slots, number, added: true);
        var dropped = Players(slots, number, added: false);
        if (teamId == null && added.Count == 0 && dropped.Count == 0)
            return null;

        return new(teamId, added, dropped);
    }

    private static List<ChatMessagePlayer> Players(List<PlayerSlot> slots, int side, bool added)
    {
        var list = new List<ChatMessagePlayer>();
        foreach (var slot in slots.Where(slot => slot.Side == side && slot.Added == added).OrderBy(slot => slot.Order))
            list.Add(new(slot.PlayerId));

        return list;
    }

    private static Dictionary<string, string>? ReadAttributes(Dictionary<string, JsonElement>? attributes)
    {
        if (attributes == null)
            return null;

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in attributes)
            values[pair.Key] = AttributeValue(pair.Value);

        return values;
    }

    private static string AttributeValue(JsonElement value) => value.ValueKind switch {
        JsonValueKind.String => value.GetString() ?? "",
        JsonValueKind.Null => "",
        _ => value.GetRawText()
    };

    private static List<PlayerSlot> ReadSlots(IReadOnlyDictionary<string, string>? attributes)
    {
        var slots = new List<PlayerSlot>();
        if (attributes == null)
            return slots;

        foreach (var pair in attributes) {
            if (TryPlayerSlot(pair.Key, pair.Value, out var slot))
                slots.Add(slot);
        }

        return slots;
    }

    private static bool TryPlayerSlot(string key, string raw, out PlayerSlot slot)
    {
        slot = default;
        var body = key;
        var side = 1;
        var tradeKey = false;
        if (body.StartsWith("team1", StringComparison.Ordinal)) {
            body = body.Substring("team1".Length);
            tradeKey = true;
        }
        else if (body.StartsWith("team2", StringComparison.Ordinal)) {
            body = body.Substring("team2".Length);
            side = 2;
            tradeKey = true;
        }

        var added = body.StartsWith("addP", StringComparison.Ordinal);
        if (!added && !body.StartsWith("dropP", StringComparison.Ordinal))
            return false;

        var number = body.Substring(added ? "addP".Length : "dropP".Length);
        if (number.Length == 0 || !int.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out var order))
            return false;
        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var playerId))
            return false;

        slot = new(side, added, order, playerId, tradeKey);
        return true;
    }

    private static string? TransactionId(IReadOnlyDictionary<string, string>? attributes)
        => attributes != null && attributes.TryGetValue("tId", out var id) && id.Length > 0 ? id : null;

    private static bool HasKey(IReadOnlyDictionary<string, string>? attributes, string key) => attributes != null && attributes.ContainsKey(key);

    private static bool TryInt(IReadOnlyDictionary<string, string>? attributes, string key, out int value)
    {
        value = 0;
        return attributes != null && attributes.TryGetValue(key, out var raw) && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private readonly record struct PlayerSlot(int Side, bool Added, int Order, int PlayerId, bool TradeKey);
}
