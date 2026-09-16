using System.Text.Json;
using Lyo.Common.Core.Enums;
using Lyo.Leaderboard.Enums;
using Lyo.Leaderboard.Postgres.Database;

namespace Lyo.Leaderboard.Postgres;

internal static class LeaderboardMapper
{
    public static string EnumText<T>(T value) where T : struct, Enum
        => JsonSerializer.Serialize(value, LeaderboardJson.Options).Trim('"');

    public static T ParseEnum<T>(string? text, T fallback) where T : struct, Enum
        => Enum.TryParse<T>(text, ignoreCase: true, out var parsed) ||
            TryParseCamel<T>(text, out parsed)
            ? parsed
            : fallback;

    private static bool TryParseCamel<T>(string? text, out T parsed) where T : struct, Enum
    {
        parsed = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var json = $"\"{text}\"";
        try {
            parsed = JsonSerializer.Deserialize<T>(json, LeaderboardJson.Options);
            return true;
        }
        catch (JsonException) {
            return false;
        }
    }

    public static BoardRecord ToBoard(BoardEntity e)
        => new() {
            Id = e.Id,
            Slug = e.Slug,
            Name = e.Name,
            Description = e.Description,
            OwnerUserId = e.OwnerUserId,
            TenantId = e.TenantId,
            Visibility = ParseEnum(e.Visibility, BoardVisibility.Private),
            JoinPolicy = ParseEnum(e.JoinPolicy, JoinPolicy.InviteOnly),
            MembersCanSubmit = e.MembersCanSubmit,
            RequireVerification = e.RequireVerification,
            MembersCanEditOwn = e.MembersCanEditOwn,
            MembersCanDeleteOwn = e.MembersCanDeleteOwn,
            AllowMultipleEntries = e.AllowMultipleEntries,
            AggregationKind = ParseEnum(e.AggregationKind, AggregationKind.Sum),
            AggregationN = e.AggregationN,
            SortDirection = ParseEnum(e.SortDirection, SortDirection.Desc),
            TieBreak = ParseEnum(e.TieBreak, TieBreakKind.FirstSubmitted),
            TieBreakPropertyKey = e.TieBreakPropertyKey,
            AlgorithmKey = e.AlgorithmKey,
            StartsAt = e.StartsAt,
            EndsAt = e.EndsAt,
            ArchivedAt = e.ArchivedAt,
            DeletedAt = e.DeletedAt,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };

    public static void CopyBoard(BoardRecord src, BoardEntity dest)
    {
        dest.Slug = src.Slug;
        dest.Name = src.Name;
        dest.Description = src.Description;
        dest.OwnerUserId = src.OwnerUserId;
        dest.TenantId = src.TenantId;
        dest.Visibility = EnumText(src.Visibility);
        dest.JoinPolicy = EnumText(src.JoinPolicy);
        dest.MembersCanSubmit = src.MembersCanSubmit;
        dest.RequireVerification = src.RequireVerification;
        dest.MembersCanEditOwn = src.MembersCanEditOwn;
        dest.MembersCanDeleteOwn = src.MembersCanDeleteOwn;
        dest.AllowMultipleEntries = src.AllowMultipleEntries;
        dest.AggregationKind = EnumText(src.AggregationKind);
        dest.AggregationN = src.AggregationN;
        dest.SortDirection = EnumText(src.SortDirection);
        dest.TieBreak = EnumText(src.TieBreak);
        dest.TieBreakPropertyKey = src.TieBreakPropertyKey;
        if (!string.IsNullOrWhiteSpace(src.AlgorithmKey))
            dest.AlgorithmKey = src.AlgorithmKey;
        dest.StartsAt = src.StartsAt;
        dest.EndsAt = src.EndsAt;
        dest.ArchivedAt = src.ArchivedAt;
        dest.DeletedAt = src.DeletedAt;
    }

    public static ScoringPropertyRecord ToProperty(ScoringPropertyEntity e)
        => new() {
            Id = e.Id,
            BoardId = e.BoardId,
            Key = e.Key,
            Label = e.Label,
            Description = e.Description,
            ValueKind = ParseEnum(e.ValueKind, ScoringValueKind.Number),
            Unit = e.Unit,
            Required = e.Required,
            SortOrder = e.SortOrder,
            MinValue = e.MinValue,
            MaxValue = e.MaxValue,
            ValidationRegex = e.ValidationRegex,
            Choices = LeaderboardJson.Deserialize(e.ChoicesJson, new List<ScoringChoice>()),
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };

    public static void CopyProperty(ScoringPropertyRecord src, ScoringPropertyEntity dest)
    {
        dest.BoardId = src.BoardId;
        dest.Key = src.Key;
        dest.Label = src.Label;
        dest.Description = src.Description;
        dest.ValueKind = EnumText(src.ValueKind);
        dest.Unit = src.Unit;
        dest.Required = src.Required;
        dest.SortOrder = src.SortOrder;
        dest.MinValue = src.MinValue;
        dest.MaxValue = src.MaxValue;
        dest.ValidationRegex = src.ValidationRegex;
        dest.ChoicesJson = src.Choices.Count == 0 ? null : LeaderboardJson.Serialize(src.Choices);
    }

    public static MemberRecord ToMember(MemberEntity e)
        => new() {
            Id = e.Id,
            BoardId = e.BoardId,
            UserId = e.UserId,
            DisplayName = e.DisplayName,
            Role = ParseEnum(e.Role, MemberRole.Contributor),
            Status = ParseEnum(e.Status, MemberStatus.Invited),
            InvitedByUserId = e.InvitedByUserId,
            JoinedAt = e.JoinedAt,
            RemovedAt = e.RemovedAt,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        };

    public static InviteRecord ToInvite(InviteEntity e)
        => new() {
            Id = e.Id,
            BoardId = e.BoardId,
            TokenHash = e.TokenHash,
            ShareKind = ParseEnum(e.ShareKind, InviteShareKind.Link),
            Email = e.Email,
            Phone = e.Phone,
            InvitedUserId = e.InvitedUserId,
            ShortUrlId = e.ShortUrlId,
            Role = ParseEnum(e.Role, MemberRole.Contributor),
            MaxUses = e.MaxUses,
            UseCount = e.UseCount,
            ExpiresAt = e.ExpiresAt,
            RevokedAt = e.RevokedAt,
            CreatedByUserId = e.CreatedByUserId,
            CreatedAt = e.CreatedAt
        };

    public static EntryRecord ToEntry(EntryEntity e)
        => new() {
            Id = e.Id,
            BoardId = e.BoardId,
            MemberId = e.MemberId,
            SubmittedByUserId = e.SubmittedByUserId,
            Title = e.Title,
            Notes = e.Notes,
            OccurredAt = e.OccurredAt,
            Status = ParseEnum(e.Status, EntryStatus.Draft),
            VerifiedByUserId = e.VerifiedByUserId,
            VerifiedAt = e.VerifiedAt,
            RejectionReason = e.RejectionReason,
            ComputedScore = e.ComputedScore,
            ScoreBreakdownJson = e.ScoreBreakdownJson,
            Values = e.Values.Select(ToValue).ToList(),
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt,
            DeletedAt = e.DeletedAt
        };

    public static EntryValueRecord ToValue(EntryValueEntity e)
        => new() {
            Id = e.Id,
            EntryId = e.EntryId,
            PropertyId = e.PropertyId,
            ChoiceKey = e.ChoiceKey,
            NumberValue = e.NumberValue,
            TextValue = e.TextValue,
            BoolValue = e.BoolValue,
            DateTimeValue = e.DateTimeValue
        };

    public static AdjustmentRecord ToAdjustment(AdjustmentEntity e)
        => new() {
            Id = e.Id,
            BoardId = e.BoardId,
            MemberId = e.MemberId,
            EntryId = e.EntryId,
            Delta = e.Delta,
            Reason = e.Reason,
            CreatedByUserId = e.CreatedByUserId,
            CreatedAt = e.CreatedAt,
            DeletedAt = e.DeletedAt
        };

    public static StandingRecord ToStanding(StandingEntity e, MemberEntity? member)
        => new() {
            BoardId = e.BoardId,
            MemberId = e.MemberId,
            EntryScore = e.EntryScore,
            AdjustmentScore = e.AdjustmentScore,
            TotalScore = e.TotalScore,
            EntryCount = e.EntryCount,
            LastEntryAt = e.LastEntryAt,
            Rank = e.Rank,
            ComputedAt = e.ComputedAt,
            DisplayName = member?.DisplayName,
            UserId = member?.UserId
        };
}
