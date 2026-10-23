using System.Diagnostics;

namespace Lyo.Espn.Fantasy.Football.Client.Models.Response;

/// <summary>How a received Fantasy Chat message is classified. Types other than text, add/drop, and trade stay <see cref="Unknown" />.</summary>
public enum ChatMessageKind
{
    /// <summary>A member or league-manager text message.</summary>
    Text,

    /// <summary>One team picked up and dropped players.</summary>
    AddDrop,

    /// <summary>Two teams exchanged players.</summary>
    Trade,

    /// <summary>A message this client does not classify.</summary>
    Unknown
}

/// <summary>
/// One player on a chat message. <see cref="PlayerId" /> is the ESPN id. <see cref="Player" /> is the card after
/// <see cref="CommunicationManager.LoadPlayersAsync(ChatConversationRes, int, int, CancellationToken)" />.
/// </summary>
public sealed class ChatMessagePlayer
{
    /// <summary>ESPN player id. Negative ids are defense and kicker slots.</summary>
    public int PlayerId { get; }

    /// <summary>Player card, when <see cref="CommunicationManager.LoadPlayersAsync(ChatConversationRes, int, int, CancellationToken)" /> has loaded it.</summary>
    public PlayerInfoItemRes? Player { get; internal set; }

    /// <summary>Builds a player reference from the ESPN id. The card stays empty until a load.</summary>
    public ChatMessagePlayer(int playerId) => PlayerId = playerId;
}

/// <summary>One side of a trade or add/drop. <see cref="TeamId" /> is the ESPN team. <see cref="Team" /> is that team when the league index had it.</summary>
public sealed class ChatMessageSide
{
    /// <summary>ESPN team id for this side.</summary>
    public int? TeamId { get; }

    /// <summary>League team for <see cref="TeamId" />. Null when the league index did not have the team.</summary>
    public TeamRes? Team { get; internal set; }

    /// <summary>Players added to this team, in ESPN's numeric order.</summary>
    public IReadOnlyList<ChatMessagePlayer> Added { get; }

    /// <summary>Players dropped from this team, in ESPN's numeric order.</summary>
    public IReadOnlyList<ChatMessagePlayer> Dropped { get; }

    /// <summary>Builds a side from the team id and the added and dropped players, in ESPN's numeric order.</summary>
    public ChatMessageSide(int? teamId, IReadOnlyList<ChatMessagePlayer> added, IReadOnlyList<ChatMessagePlayer> dropped)
    {
        TeamId = teamId;
        Added = added;
        Dropped = dropped;
    }
}

/// <summary>
/// One Fantasy Chat message. <see cref="AuthorId" /> and <see cref="DateUnix" /> are the ESPN values. <see cref="Author" /> and <see cref="Date" /> are those values in a
/// form you can use. Adds, drops, and trades list players on <see cref="Side1" /> and <see cref="Side2" />.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ChatMessageRes
{
    /// <summary>ESPN message id.</summary>
    public string Id { get; }

    /// <summary>Author SWID, or <c>LM</c> for the league manager.</summary>
    public string? AuthorId { get; }

    /// <summary>League member for <see cref="AuthorId" />. Null for <c>LM</c>, a blank id, or a member the league index did not have.</summary>
    public MemberRes? Author { get; internal set; }

    /// <summary>True when <see cref="AuthorId" /> is the league-manager marker <c>LM</c>.</summary>
    public bool FromLeagueManager => LeagueIndex.IsLeagueManager(AuthorId);

    /// <summary>ESPN <c>date</c> as unix milliseconds.</summary>
    public long DateUnix { get; }

    /// <summary><see cref="DateUnix" /> as a timestamp.</summary>
    public DateTimeOffset Date => EspnUnix.ToOffset(DateUnix);

    /// <summary>True when ESPN has deleted the message.</summary>
    public bool IsDeleted { get; }

    /// <summary>Topic id this message belongs to.</summary>
    public string? TopicId { get; }

    /// <summary>Text, add/drop, trade, or unknown.</summary>
    public ChatMessageKind Kind { get; }

    /// <summary>Text body. Null for trades and add/drops.</summary>
    public string? Content { get; }

    /// <summary>ESPN transaction id (<c>tId</c>) for a trade.</summary>
    public string? TransactionId { get; }

    /// <summary>First team on a trade or the only team on an add/drop.</summary>
    public ChatMessageSide? Side1 { get; }

    /// <summary>Second team on a trade. Null for an add/drop that has no <c>team2</c>.</summary>
    public ChatMessageSide? Side2 { get; }

    /// <summary>True when ESPN has edited the message.</summary>
    public bool IsEdited { get; }

    /// <summary>Builds a message from values already taken off the ESPN payload.</summary>
    public ChatMessageRes(
        string Id,
        string? AuthorId,
        long DateUnix,
        bool IsDeleted,
        string? TopicId,
        ChatMessageKind Kind,
        string? Content = null,
        string? TransactionId = null,
        ChatMessageSide? Side1 = null,
        ChatMessageSide? Side2 = null,
        bool IsEdited = false)
    {
        this.Id = Id;
        this.AuthorId = AuthorId;
        this.DateUnix = DateUnix;
        this.IsDeleted = IsDeleted;
        this.TopicId = TopicId;
        this.Kind = Kind;
        this.Content = Content;
        this.TransactionId = TransactionId;
        this.Side1 = Side1;
        this.Side2 = Side2;
        this.IsEdited = IsEdited;
    }

    /// <summary>Id, kind, and author, for logs and the debugger.</summary>
    public override string ToString() => $"Message Id={Id} Kind={Kind} Author={AuthorId} Date={DateUnix}";

    internal void CollectPlayerIds(List<int> playerIds)
    {
        Collect(Side1, playerIds);
        Collect(Side2, playerIds);
    }

    internal void ApplyPlayers(IReadOnlyDictionary<int, PlayerInfoItemRes> cards)
    {
        Apply(Side1, cards);
        Apply(Side2, cards);
    }

    private static void Collect(ChatMessageSide? side, List<int> playerIds)
    {
        if (side == null)
            return;

        foreach (var player in side.Added) {
            if (player.Player == null)
                playerIds.Add(player.PlayerId);
        }

        foreach (var player in side.Dropped) {
            if (player.Player == null)
                playerIds.Add(player.PlayerId);
        }
    }

    private static void Apply(ChatMessageSide? side, IReadOnlyDictionary<int, PlayerInfoItemRes> cards)
    {
        if (side == null)
            return;

        foreach (var player in side.Added)
            Assign(player, cards);

        foreach (var player in side.Dropped)
            Assign(player, cards);
    }

    private static void Assign(ChatMessagePlayer player, IReadOnlyDictionary<int, PlayerInfoItemRes> cards)
    {
        if (player.Player == null && cards.TryGetValue(player.PlayerId, out var card))
            player.Player = card;
    }
}