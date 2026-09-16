using Lyo.Authentication.AspNetCore.Authorization;
using Lyo.Authentication.Services.Users;
using Lyo.Email;
using Lyo.Leaderboard;
using Lyo.Email.Builders;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Leaderboard.Api.Models;
using Lyo.Leaderboard.Enums;
using Lyo.QRCode;
using Lyo.QRCode.Models;
using Lyo.ShortUrl;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lyo.Leaderboard.Api.Endpoints;

internal static class LeaderboardEndpoints
{
    public static void MapLeaderboardRoutes(this RouteGroupBuilder group, bool requireAuthentication)
    {
        if (requireAuthentication) {
            group.MapPost("/boards", CreateBoard).RequireScope(LeaderboardScopes.Write);
            group.MapPut("/boards/{boardId:guid}", UpdateBoard).RequireScope(LeaderboardScopes.Write);
            group.MapPost("/boards/{boardId:guid}/archive", ArchiveBoard).RequireScope(LeaderboardScopes.Write);
            group.MapPut("/boards/{boardId:guid}/properties", SaveProperty).RequireScope(LeaderboardScopes.Write);
            group.MapDelete("/boards/{boardId:guid}/properties/{propertyId:guid}", DeleteProperty).RequireScope(LeaderboardScopes.Write);
            group.MapPost("/boards/{boardId:guid}/invites", CreateInvite).RequireScope(LeaderboardScopes.Write);
            group.MapPost("/invites/{inviteId:guid}/revoke", RevokeInvite).RequireScope(LeaderboardScopes.Write);
            group.MapGet("/invites/{inviteId:guid}/qr", GetInviteQr).RequireScope(LeaderboardScopes.Read);
            group.MapPost("/join", RedeemInvite).RequireScope(LeaderboardScopes.Write);
            group.MapPost("/boards/{boardId:guid}/entries", CreateEntry).RequireScope(LeaderboardScopes.Write);
            group.MapPost("/entries/{entryId:guid}/status", SetEntryStatus).RequireScope(LeaderboardScopes.Write);
            group.MapPost("/boards/{boardId:guid}/adjustments", AddAdjustment).RequireScope(LeaderboardScopes.Write);
            group.MapPost("/adjustments/{adjustmentId:guid}/undo", UndoAdjustment).RequireScope(LeaderboardScopes.Write);
            group.MapPost("/boards/{boardId:guid}/standings/recalculate", Recalculate).RequireScope(LeaderboardScopes.Write);
        }
        else {
            group.MapPost("/boards", CreateBoard);
            group.MapPut("/boards/{boardId:guid}", UpdateBoard);
            group.MapPost("/boards/{boardId:guid}/archive", ArchiveBoard);
            group.MapPut("/boards/{boardId:guid}/properties", SaveProperty);
            group.MapDelete("/boards/{boardId:guid}/properties/{propertyId:guid}", DeleteProperty);
            group.MapPost("/boards/{boardId:guid}/invites", CreateInvite);
            group.MapPost("/invites/{inviteId:guid}/revoke", RevokeInvite);
            group.MapGet("/invites/{inviteId:guid}/qr", GetInviteQr);
            group.MapPost("/join", RedeemInvite);
            group.MapPost("/boards/{boardId:guid}/entries", CreateEntry);
            group.MapPost("/entries/{entryId:guid}/status", SetEntryStatus);
            group.MapPost("/boards/{boardId:guid}/adjustments", AddAdjustment);
            group.MapPost("/adjustments/{adjustmentId:guid}/undo", UndoAdjustment);
            group.MapPost("/boards/{boardId:guid}/standings/recalculate", Recalculate);
        }

        var reads = group.MapGroup(string.Empty);
        if (requireAuthentication)
            reads.RequireScope(LeaderboardScopes.Read);

        reads.MapGet("/boards/{boardId:guid}", GetBoard);
        reads.MapGet("/boards/slug/{slug}", GetBoardBySlug);
        reads.MapGet("/boards/{boardId:guid}/properties", ListProperties);
        reads.MapGet("/boards/{boardId:guid}/members", ListMembers);
        reads.MapGet("/boards/{boardId:guid}/entries", ListEntries);
        reads.MapGet("/boards/{boardId:guid}/standings", GetStandings);
        reads.MapGet("/entries/{entryId:guid}", GetEntry);
    }

    public static void MapShortUrlRedirect(this WebApplication app)
        => app.MapGet(
                "/s/{id}", async (string id, HttpContext http, CancellationToken ct) => {
                    var shorts = http.RequestServices.GetService<IShortUrlService>();
                    if (shorts == null)
                        return Results.NotFound();

                    var expanded = await shorts.ExpandAsync(id, ct).ConfigureAwait(false);
                    if (!expanded.IsSuccess || string.IsNullOrWhiteSpace(expanded.LongUrl))
                        return Results.NotFound();

                    return Results.Redirect(expanded.LongUrl);
                })
            .AllowAnonymous()
            .WithTags("ShortUrl");

    private static async Task<IResult> CreateBoard(
        CreateBoardRequest req,
        ILeaderboardStore store,
        IUserStore users,
        HttpContext http,
        CancellationToken ct)
    {
        var actor = await LeaderboardActorResolver.ResolveAsync(http, users, ct).ConfigureAwait(false);
        var saved = await store.SaveBoardAsync(ToBoard(req, Guid.Empty), actor, ct).ConfigureAwait(false);
        return Results.Created($"/api/leaderboard/boards/{saved.Id}", saved);
    }

    private static async Task<IResult> UpdateBoard(
        Guid boardId,
        CreateBoardRequest req,
        ILeaderboardStore store,
        IUserStore users,
        HttpContext http,
        CancellationToken ct)
    {
        var actor = await LeaderboardActorResolver.ResolveAsync(http, users, ct).ConfigureAwait(false);
        var existing = await store.GetBoardByIdAsync(boardId, actor, ct).ConfigureAwait(false);
        if (existing == null)
            return Results.NotFound();

        var updated = ToBoard(req, boardId);
        updated.Slug = string.IsNullOrWhiteSpace(req.Slug) ? existing.Slug : req.Slug!;
        return Results.Ok(await store.SaveBoardAsync(updated, actor, ct).ConfigureAwait(false));
    }

    private static async Task<IResult> ArchiveBoard(Guid boardId, ILeaderboardStore store, IUserStore users, HttpContext http, CancellationToken ct)
    {
        var actor = await LeaderboardActorResolver.ResolveAsync(http, users, ct).ConfigureAwait(false);
        await store.ArchiveBoardAsync(boardId, actor, ct).ConfigureAwait(false);
        return Results.NoContent();
    }

    private static async Task<IResult> GetBoard(Guid boardId, ILeaderboardStore store, IUserStore users, HttpContext http, CancellationToken ct)
    {
        var actor = await LeaderboardActorResolver.ResolveAsync(http, users, ct).ConfigureAwait(false);
        var board = await store.GetBoardByIdAsync(boardId, actor, ct).ConfigureAwait(false);
        return board == null ? Results.NotFound() : Results.Ok(board);
    }

    private static async Task<IResult> GetBoardBySlug(string slug, ILeaderboardStore store, IUserStore users, HttpContext http, CancellationToken ct)
    {
        var actor = await LeaderboardActorResolver.ResolveAsync(http, users, ct).ConfigureAwait(false);
        var board = await store.GetBoardBySlugAsync(slug, actor, ct).ConfigureAwait(false);
        return board == null ? Results.NotFound() : Results.Ok(board);
    }

    private static async Task<IResult> SaveProperty(
        Guid boardId,
        ScoringPropertyRecord property,
        ILeaderboardStore store,
        IUserStore users,
        HttpContext http,
        CancellationToken ct)
    {
        var actor = await LeaderboardActorResolver.ResolveAsync(http, users, ct).ConfigureAwait(false);
        property.BoardId = boardId;
        return Results.Ok(await store.SavePropertyAsync(property, actor, ct).ConfigureAwait(false));
    }

    private static async Task<IResult> DeleteProperty(Guid boardId, Guid propertyId, ILeaderboardStore store, IUserStore users, HttpContext http, CancellationToken ct)
    {
        var actor = await LeaderboardActorResolver.ResolveAsync(http, users, ct).ConfigureAwait(false);
        await store.DeletePropertyAsync(propertyId, actor, ct).ConfigureAwait(false);
        return Results.NoContent();
    }

    private static async Task<IResult> ListProperties(Guid boardId, ILeaderboardStore store, IUserStore users, HttpContext http, CancellationToken ct)
    {
        var actor = await LeaderboardActorResolver.ResolveAsync(http, users, ct).ConfigureAwait(false);
        return Results.Ok(await store.ListPropertiesAsync(boardId, actor, ct).ConfigureAwait(false));
    }

    private static async Task<IResult> ListMembers(Guid boardId, ILeaderboardStore store, IUserStore users, HttpContext http, CancellationToken ct)
    {
        var actor = await LeaderboardActorResolver.ResolveAsync(http, users, ct).ConfigureAwait(false);
        return Results.Ok(await store.ListMembersAsync(boardId, actor, ct).ConfigureAwait(false));
    }

    private static async Task<IResult> CreateInvite(
        Guid boardId,
        CreateInviteApiRequest req,
        ILeaderboardStore store,
        IUserStore users,
        IOptions<LeaderboardApiOptions> options,
        HttpContext http,
        CancellationToken ct)
    {
        var shorts = http.RequestServices.GetService<IShortUrlService>();
        var email = http.RequestServices.GetService<IEmailService>();
        var actor = await LeaderboardActorResolver.ResolveAsync(http, users, ct).ConfigureAwait(false);
        if (req.ShareKind == InviteShareKind.Email && !string.IsNullOrWhiteSpace(req.Email)) {
            var existing = await users.GetByEmailAsync(req.Email, null, ct).ConfigureAwait(false);
            if (existing != null) {
                req.InvitedUserId = existing.Id;
                req.DisplayName ??= existing.DisplayName;
            }
        }

        if (req.ShareKind == InviteShareKind.User && req.InvitedUserId is { } invitedId) {
            var invited = await users.GetByIdAsync(invitedId, null, ct).ConfigureAwait(false);
            req.Email ??= invited?.Email;
            req.DisplayName ??= invited?.DisplayName;
        }

        var created = await store.CreateInviteAsync(
                boardId,
                new() {
                    ShareKind = req.ShareKind,
                    Role = req.Role,
                    Email = req.Email,
                    Phone = req.Phone,
                    InvitedUserId = req.InvitedUserId,
                    DisplayName = req.DisplayName,
                    ExpiresAt = req.ExpiresAt
                }, actor, ct)
            .ConfigureAwait(false);
        var board = await store.GetBoardByIdAsync(boardId, actor, ct).ConfigureAwait(false);
        var joinUrl = LeaderboardJoinUrl.BuildAbsolute(options.Value.PublicBaseUrl, board?.Slug ?? boardId.ToString("N"), created.Token);

        string? shortUrl = null;
        if (shorts != null) {
            var shortened = await shorts.ShortenAsync(joinUrl, expirationDate: created.Invite.ExpiresAt, ct: ct).ConfigureAwait(false);
            if (shortened.IsSuccess && !string.IsNullOrWhiteSpace(shortened.ShortUrl)) {
                shortUrl = shortened.ShortUrl;
                var id = shortened.ShortUrl.Trim().TrimEnd('/');
                var slash = id.LastIndexOf('/');
                await store.AttachShortUrlAsync(created.Invite.Id, slash < 0 ? id : id[(slash + 1)..], actor, ct).ConfigureAwait(false);
                created.Invite.ShortUrlId = slash < 0 ? id : id[(slash + 1)..];
            }
        }

        if (options.Value.SendInviteEmail && email != null && !string.IsNullOrWhiteSpace(created.Invite.Email)) {
            var link = shortUrl ?? joinUrl;
            await email.SendEmailAsync(
                    EmailRequestBuilder.New()
                        .SetSubject("You're invited to a leaderboard")
                        .SetTextBody($"Join: {link}")
                        .SetHtmlBody($"<p>Join the leaderboard:</p><p><a href=\"{link}\">{link}</a></p>")
                        .AddTo(created.Invite.Email, created.Member?.DisplayName ?? created.Invite.Email), ct)
                .ConfigureAwait(false);
        }

        return Results.Ok(new InviteCreatedResponse {
            Invite = created.Invite,
            Token = created.Token,
            JoinPath = created.JoinPath,
            JoinUrl = joinUrl,
            ShortUrl = shortUrl,
            Member = created.Member
        });
    }

    private static async Task<IResult> RevokeInvite(Guid inviteId, ILeaderboardStore store, IUserStore users, HttpContext http, CancellationToken ct)
    {
        var actor = await LeaderboardActorResolver.ResolveAsync(http, users, ct).ConfigureAwait(false);
        await store.RevokeInviteAsync(inviteId, actor, ct).ConfigureAwait(false);
        return Results.NoContent();
    }

    private static async Task<IResult> GetInviteQr(
        Guid inviteId,
        ILeaderboardStore store,
        IUserStore users,
        IQRCodeService qr,
        IOptions<LeaderboardApiOptions> options,
        HttpContext http,
        CancellationToken ct)
    {
        var shorts = http.RequestServices.GetService<IShortUrlService>();
        var actor = await LeaderboardActorResolver.ResolveAsync(http, users, ct).ConfigureAwait(false);
        var invite = await store.GetInviteByIdAsync(inviteId, actor, ct).ConfigureAwait(false);
        if (invite == null)
            return Results.NotFound();

        string? payload = null;
        if (!string.IsNullOrWhiteSpace(invite.ShortUrlId)) {
            if (shorts != null)
                payload = (await shorts.ExpandAsync(invite.ShortUrlId, ct).ConfigureAwait(false)).LongUrl;

            payload ??= $"{options.Value.PublicBaseUrl.TrimEnd('/')}/s/{invite.ShortUrlId}";
        }

        if (string.IsNullOrWhiteSpace(payload))
            throw new BadRequestException("Invite has no short URL to encode. Create the invite with ShortUrl configured.");
        var generated = await qr.GenerateAsync(payload, new QRCodeOptions { Format = QRCodeFormat.Png, Size = 12 }, ct).ConfigureAwait(false);
        if (generated is not QRCodeResult qrResult || qrResult.ImageBytes == null)
            throw new BadRequestException("QR generation failed.");

        return Results.File(qrResult.ImageBytes, "image/png", "invite-qr.png");
    }

    private static async Task<IResult> RedeemInvite(RedeemInviteRequest req, ILeaderboardStore store, IUserStore users, HttpContext http, CancellationToken ct)
    {
        var actor = await LeaderboardActorResolver.ResolveAsync(http, users, ct).ConfigureAwait(false);
        return Results.Ok(await store.RedeemInviteAsync(req.Token, actor, ct).ConfigureAwait(false));
    }

    private static async Task<IResult> CreateEntry(
        Guid boardId,
        CreateEntryRequest req,
        ILeaderboardStore store,
        IUserStore users,
        HttpContext http,
        CancellationToken ct)
    {
        var actor = await LeaderboardActorResolver.ResolveAsync(http, users, ct).ConfigureAwait(false);
        var entry = new EntryRecord {
            BoardId = boardId,
            MemberId = req.MemberId,
            Title = req.Title,
            Notes = req.Notes,
            OccurredAt = req.OccurredAt,
            Values = req.Values
        };
        return Results.Ok(await store.SaveEntryAsync(entry, actor, ct).ConfigureAwait(false));
    }

    private static async Task<IResult> GetEntry(Guid entryId, ILeaderboardStore store, IUserStore users, HttpContext http, CancellationToken ct)
    {
        var actor = await LeaderboardActorResolver.ResolveAsync(http, users, ct).ConfigureAwait(false);
        var entry = await store.GetEntryByIdAsync(entryId, actor, ct).ConfigureAwait(false);
        return entry == null ? Results.NotFound() : Results.Ok(entry);
    }

    private static async Task<IResult> ListEntries(Guid boardId, ILeaderboardStore store, IUserStore users, HttpContext http, CancellationToken ct)
    {
        var actor = await LeaderboardActorResolver.ResolveAsync(http, users, ct).ConfigureAwait(false);
        return Results.Ok(await store.ListEntriesAsync(boardId, actor, ct).ConfigureAwait(false));
    }

    private static async Task<IResult> SetEntryStatus(
        Guid entryId,
        EntryStatusChange change,
        ILeaderboardStore store,
        IUserStore users,
        HttpContext http,
        CancellationToken ct)
    {
        var actor = await LeaderboardActorResolver.ResolveAsync(http, users, ct).ConfigureAwait(false);
        return Results.Ok(await store.SetEntryStatusAsync(entryId, change, actor, ct).ConfigureAwait(false));
    }

    private static async Task<IResult> AddAdjustment(
        Guid boardId,
        CreateAdjustmentRequest req,
        ILeaderboardStore store,
        IUserStore users,
        HttpContext http,
        CancellationToken ct)
    {
        var actor = await LeaderboardActorResolver.ResolveAsync(http, users, ct).ConfigureAwait(false);
        var saved = await store.AddAdjustmentAsync(
                new() { BoardId = boardId, MemberId = req.MemberId, EntryId = req.EntryId, Delta = req.Delta, Reason = req.Reason }, actor, ct)
            .ConfigureAwait(false);
        return Results.Ok(saved);
    }

    private static async Task<IResult> UndoAdjustment(Guid adjustmentId, ILeaderboardStore store, IUserStore users, HttpContext http, CancellationToken ct)
    {
        var actor = await LeaderboardActorResolver.ResolveAsync(http, users, ct).ConfigureAwait(false);
        await store.UndoAdjustmentAsync(adjustmentId, actor, ct).ConfigureAwait(false);
        return Results.NoContent();
    }

    private static async Task<IResult> GetStandings(Guid boardId, ILeaderboardStore store, IUserStore users, HttpContext http, CancellationToken ct)
    {
        var actor = await LeaderboardActorResolver.ResolveAsync(http, users, ct).ConfigureAwait(false);
        return Results.Ok(await store.GetStandingsAsync(boardId, actor, ct).ConfigureAwait(false));
    }

    private static async Task<IResult> Recalculate(Guid boardId, ILeaderboardStore store, IUserStore users, HttpContext http, CancellationToken ct)
    {
        var actor = await LeaderboardActorResolver.ResolveAsync(http, users, ct).ConfigureAwait(false);
        await store.RecalculateStandingsAsync(boardId, actor, ct).ConfigureAwait(false);
        return Results.NoContent();
    }

    private static BoardRecord ToBoard(CreateBoardRequest req, Guid id)
        => new() {
            Id = id,
            Name = req.Name,
            Description = req.Description,
            Slug = req.Slug ?? string.Empty,
            Visibility = req.Visibility,
            JoinPolicy = req.JoinPolicy,
            MembersCanSubmit = req.MembersCanSubmit,
            RequireVerification = req.RequireVerification,
            MembersCanEditOwn = req.MembersCanEditOwn,
            MembersCanDeleteOwn = req.MembersCanDeleteOwn,
            AllowMultipleEntries = req.AllowMultipleEntries,
            AggregationKind = req.AggregationKind,
            AggregationN = req.AggregationN,
            SortDirection = req.SortDirection,
            TieBreak = req.TieBreak,
            TieBreakPropertyKey = req.TieBreakPropertyKey,
            ScoreFormula = req.ScoreFormula,
            StartsAt = req.StartsAt,
            EndsAt = req.EndsAt
        };
}
