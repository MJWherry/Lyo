using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions.Models;
using Lyo.Leaderboard.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Leaderboard.Postgres.Tests;

[Trait("Category", "Integration")]
public sealed class FishingBoardStoreTests(LeaderboardPostgresFixture fixture)
{
    private const string DerbyFormula = """
                                        fish_type + (weight >= 5 && weight < 10 ? 3 : (weight >= 0 && weight < 5 ? 1 : 0)) + (length < 10 ? 0 : (length > 20 ? 50 : (length - 10) / 10 * 50))
                                        """;

    [Fact]
    public async Task FishingBoard_ScoreVerifyAdjustRank_AndEmailLockedRedeem()
    {
        var store = fixture.ServiceProvider.GetRequiredService<ILeaderboardStore>();
        var owner = new LeaderboardActor(LyoGuid.CreateV7(), "owner@derby.test", "Owner");
        var angler = new LeaderboardActor(LyoGuid.CreateV7(), "angler@derby.test", "Angler");
        var stranger = new LeaderboardActor(LyoGuid.CreateV7(), "other@derby.test", "Stranger");
        var board = await store.SaveBoardAsync(
            new() {
                Name = "Spring Fishing Derby",
                Description = "Bass weekend",
                Visibility = BoardVisibility.Unlisted,
                RequireVerification = true,
                AllowMultipleEntries = true,
                ScoreFormula = DerbyFormula
            }, owner, TestCt);

        Assert.NotEqual(Guid.Empty, board.Id);
        Assert.False(string.IsNullOrWhiteSpace(board.Slug));
        var fish = await store.SavePropertyAsync(FishType(board.Id), owner, TestCt);
        var weight = await store.SavePropertyAsync(Weight(board.Id), owner, TestCt);
        var length = await store.SavePropertyAsync(Length(board.Id), owner, TestCt);
        var link = await store.CreateInviteAsync(board.Id, new() { ShareKind = InviteShareKind.Link }, owner, TestCt);
        var anglerMember = await store.RedeemInviteAsync(link.Token, angler, TestCt);
        Assert.Equal(MemberStatus.Active, anglerMember.Status);
        Assert.Equal(MemberRole.Contributor, anglerMember.Role);
        var emailInvite = await store.CreateInviteAsync(
            board.Id, new() { ShareKind = InviteShareKind.Email, Email = "locked@derby.test", Role = MemberRole.Viewer }, owner, TestCt);

        var locked = await Assert.ThrowsAsync<BadRequestException>(() => store.RedeemInviteAsync(emailInvite.Token, stranger, TestCt));
        Assert.Contains("email", locked.Message, StringComparison.OrdinalIgnoreCase);
        var lockedMember = await store.RedeemInviteAsync(emailInvite.Token, new(LyoGuid.CreateV7(), "locked@derby.test", "Locked"), TestCt);
        Assert.Equal(MemberRole.Viewer, lockedMember.Role);
        var pending = await store.SaveEntryAsync(
            Catch(board.Id, anglerMember.Id, fish.Id, weight.Id, length.Id, "bass", 6m, 15m, new(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc)), angler, TestCt);

        Assert.Equal(EntryStatus.Pending, pending.Status);
        Assert.Equal(38m, pending.ComputedScore);
        var standingsBefore = await store.GetStandingsAsync(board.Id, owner, TestCt);
        Assert.DoesNotContain(standingsBefore, s => s.MemberId == anglerMember.Id && s.EntryCount > 0);
        var approved = await store.SetEntryStatusAsync(pending.Id, new() { Status = EntryStatus.Approved }, owner, TestCt);
        Assert.Equal(EntryStatus.Approved, approved.Status);
        var ownerMembers = await store.ListMembersAsync(board.Id, owner, TestCt);
        var ownerMember = Assert.Single(ownerMembers, m => m.UserId == owner.UserId);
        var ownerEntry = await store.SaveEntryAsync(
            Catch(board.Id, ownerMember.Id, fish.Id, weight.Id, length.Id, "catfish", 3m, 10m, new(2026, 4, 1, 13, 0, 0, DateTimeKind.Utc)), owner, TestCt);

        Assert.Equal(EntryStatus.Approved, ownerEntry.Status);
        Assert.Equal(6m, ownerEntry.ComputedScore);
        var ranked = await store.GetStandingsAsync(board.Id, owner, TestCt);
        Assert.Equal(2, ranked.Count(s => s.EntryCount > 0));
        var first = ranked[0];
        var second = ranked[1];
        Assert.Equal(anglerMember.Id, first.MemberId);
        Assert.Equal(1, first.Rank);
        Assert.Equal(38m, first.TotalScore);
        Assert.Equal(ownerMember.Id, second.MemberId);
        Assert.Equal(2, second.Rank);
        Assert.Equal(6m, second.TotalScore);
        var bump = await store.AddAdjustmentAsync(
            new() {
                BoardId = board.Id,
                MemberId = ownerMember.Id,
                Delta = 40m,
                Reason = "Judge bonus"
            }, owner, TestCt);

        Assert.Equal(40m, bump.Delta);
        var afterAdjust = await store.GetStandingsAsync(board.Id, owner, TestCt);
        Assert.Equal(ownerMember.Id, afterAdjust[0].MemberId);
        Assert.Equal(1, afterAdjust[0].Rank);
        Assert.Equal(46m, afterAdjust[0].TotalScore);
        Assert.Equal(40m, afterAdjust[0].AdjustmentScore);
        Assert.Equal(anglerMember.Id, afterAdjust[1].MemberId);
        Assert.Equal(2, afterAdjust[1].Rank);
    }

    [Fact]
    public async Task FishingBoard_UnknownChoice_IsRejected_AndMissingNumber_IsZero()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<ILeaderboardStore>();
        var owner = new LeaderboardActor(LyoGuid.CreateV7(), "owner@derby.test", "Owner");
        var board = await store.SaveBoardAsync(new() { Name = "Optional bonus", RequireVerification = false, ScoreFormula = "weight + bonus" }, owner, ct);
        var weight = await store.SavePropertyAsync(Weight(board.Id), owner, ct);
        await store.SavePropertyAsync(
            new() {
                BoardId = board.Id,
                Key = "bonus",
                Label = "Bonus",
                ValueKind = ScoringValueKind.Number,
                Required = false,
                SortOrder = 2
            }, owner, ct);

        var fish = await store.SavePropertyAsync(FishType(board.Id), owner, ct);
        var members = await store.ListMembersAsync(board.Id, owner, ct);
        var ownerMember = Assert.Single(members, m => m.UserId == owner.UserId);
        var scored = await store.SaveEntryAsync(
            new() {
                BoardId = board.Id,
                MemberId = ownerMember.Id,
                OccurredAt = DateTime.UtcNow,
                Values = [new() { PropertyId = fish.Id, ChoiceKey = "bass" }, new() { PropertyId = weight.Id, NumberValue = 6m }]
            }, owner, ct);

        Assert.Equal(6m, scored.ComputedScore);
        var unknown = await Assert.ThrowsAsync<BadRequestException>(() => store.SaveEntryAsync(
            new() {
                BoardId = board.Id,
                MemberId = ownerMember.Id,
                OccurredAt = DateTime.UtcNow,
                Values = [new() { PropertyId = fish.Id, ChoiceKey = "trout" }, new() { PropertyId = weight.Id, NumberValue = 6m }]
            }, owner, ct));

        Assert.Contains("trout", unknown.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static ScoringPropertyRecord FishType(Guid boardId)
        => new() {
            BoardId = boardId,
            Key = "fish_type",
            Label = "Fish",
            ValueKind = ScoringValueKind.Choice,
            Required = true,
            SortOrder = 0,
            Choices = [new() { Key = "bass", Label = "Bass", Value = 10 }, new() { Key = "catfish", Label = "Catfish", Value = 5 }]
        };

    private static ScoringPropertyRecord Weight(Guid boardId)
        => new() {
            BoardId = boardId,
            Key = "weight",
            Label = "Weight",
            Unit = "lb",
            ValueKind = ScoringValueKind.Number,
            Required = true,
            SortOrder = 1
        };

    private static ScoringPropertyRecord Length(Guid boardId)
        => new() {
            BoardId = boardId,
            Key = "length",
            Label = "Length",
            Unit = "in",
            ValueKind = ScoringValueKind.Number,
            Required = true,
            SortOrder = 2
        };

    private static EntryRecord Catch(Guid boardId, Guid memberId, Guid fishId, Guid weightId, Guid lengthId, string fish, decimal weight, decimal length, DateTime occurredAt)
        => new() {
            BoardId = boardId,
            MemberId = memberId,
            OccurredAt = occurredAt,
            Values = [new() { PropertyId = fishId, ChoiceKey = fish }, new() { PropertyId = weightId, NumberValue = weight }, new() { PropertyId = lengthId, NumberValue = length }]
        };
}