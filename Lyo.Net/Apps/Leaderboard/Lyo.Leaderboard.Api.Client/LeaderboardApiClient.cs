using Lyo.Api.Client;
using Lyo.Exceptions;
using Lyo.Http.Client;
using Lyo.Leaderboard;
using Lyo.Leaderboard.Api.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Lyo.Leaderboard.Api.Client;

/// <summary>HTTP client surface for the Leaderboard API.</summary>
public interface ILeaderboardApiClient : IApiClient
{
    /// <summary>Creates a board.</summary>
    Task<BoardRecord> CreateBoardAsync(CreateBoardRequest request, CancellationToken ct = default);

    /// <summary>Gets a board by id.</summary>
    Task<BoardRecord?> GetBoardAsync(Guid boardId, CancellationToken ct = default);

    /// <summary>Gets a board by slug.</summary>
    Task<BoardRecord?> GetBoardBySlugAsync(string slug, CancellationToken ct = default);

    /// <summary>Updates an existing board.</summary>
    Task<BoardRecord> UpdateBoardAsync(Guid boardId, CreateBoardRequest request, CancellationToken ct = default);

    /// <summary>Archives a board.</summary>
    Task ArchiveBoardAsync(Guid boardId, CancellationToken ct = default);

    /// <summary>Lists scoring properties for a board.</summary>
    Task<IReadOnlyList<ScoringPropertyRecord>> ListPropertiesAsync(Guid boardId, CancellationToken ct = default);

    /// <summary>Creates or updates a scoring property.</summary>
    Task<ScoringPropertyRecord> SavePropertyAsync(Guid boardId, ScoringPropertyRecord property, CancellationToken ct = default);

    /// <summary>Deletes a scoring property.</summary>
    Task DeletePropertyAsync(Guid boardId, Guid propertyId, CancellationToken ct = default);

    /// <summary>Lists members for a board.</summary>
    Task<IReadOnlyList<MemberRecord>> ListMembersAsync(Guid boardId, CancellationToken ct = default);

    /// <summary>Lists standings for a board.</summary>
    Task<IReadOnlyList<StandingRecord>> GetStandingsAsync(Guid boardId, CancellationToken ct = default);

    /// <summary>Creates an invite.</summary>
    Task<InviteCreatedResponse> CreateInviteAsync(Guid boardId, CreateInviteApiRequest request, CancellationToken ct = default);

    /// <summary>Revokes an invite.</summary>
    Task RevokeInviteAsync(Guid inviteId, CancellationToken ct = default);

    /// <summary>Downloads the invite QR PNG.</summary>
    Task<byte[]> GetInviteQrAsync(Guid inviteId, CancellationToken ct = default);

    /// <summary>Redeems an invite token. Set <paramref name="asGuest" /> on anonymous hosts to act as the workbench guest.</summary>
    Task<MemberRecord> RedeemInviteAsync(string token, bool asGuest = false, CancellationToken ct = default);

    /// <summary>Submits an entry. Set <paramref name="asGuest" /> on anonymous hosts to submit as the workbench guest.</summary>
    Task<EntryRecord> CreateEntryAsync(Guid boardId, CreateEntryRequest request, bool asGuest = false, CancellationToken ct = default);

    /// <summary>Lists entries the caller can see on a board.</summary>
    Task<IReadOnlyList<EntryRecord>> ListEntriesAsync(Guid boardId, CancellationToken ct = default);

    /// <summary>Gets one entry.</summary>
    Task<EntryRecord?> GetEntryAsync(Guid entryId, CancellationToken ct = default);

    /// <summary>Approves, rejects, or withdraws an entry.</summary>
    Task<EntryRecord> SetEntryStatusAsync(Guid entryId, EntryStatusChange change, CancellationToken ct = default);

    /// <summary>Adds a manual point adjustment.</summary>
    Task<AdjustmentRecord> AddAdjustmentAsync(Guid boardId, CreateAdjustmentRequest request, CancellationToken ct = default);

    /// <summary>Undoes a live adjustment.</summary>
    Task UndoAdjustmentAsync(Guid adjustmentId, CancellationToken ct = default);

    /// <summary>Recomputes standings for a board.</summary>
    Task RecalculateStandingsAsync(Guid boardId, CancellationToken ct = default);
}

/// <summary>Typed HTTP client for the Leaderboard API.</summary>
public sealed class LeaderboardApiClient : ApiClient, ILeaderboardApiClient
{
    /// <summary>Creates the typed client.</summary>
    public LeaderboardApiClient(HttpClient httpClient, IOptions<LeaderboardApiClientOptions> options, System.Text.Json.JsonSerializerOptions? serializerOptions = null)
        : base(httpClient: httpClient, options: options.Value, serializerOptions: serializerOptions) { }

    /// <inheritdoc />
    public Task<BoardRecord> CreateBoardAsync(CreateBoardRequest request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        return PostAsAsync<CreateBoardRequest, BoardRecord>("api/leaderboard/boards", request, ct: ct);
    }

    /// <inheritdoc />
    public Task<BoardRecord?> GetBoardAsync(Guid boardId, CancellationToken ct = default)
        => GetAsAsync<BoardRecord>($"api/leaderboard/boards/{boardId:D}", ct: ct);

    /// <inheritdoc />
    public Task<BoardRecord?> GetBoardBySlugAsync(string slug, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(slug);
        return GetAsAsync<BoardRecord>($"api/leaderboard/boards/slug/{Uri.EscapeDataString(slug.Trim())}", ct: ct);
    }

    /// <inheritdoc />
    public Task<BoardRecord> UpdateBoardAsync(Guid boardId, CreateBoardRequest request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        return PutAsAsync<CreateBoardRequest, BoardRecord>($"api/leaderboard/boards/{boardId:D}", request, ct: ct);
    }

    /// <inheritdoc />
    public Task ArchiveBoardAsync(Guid boardId, CancellationToken ct = default)
        => PostExpectingNoContentAsync<object>($"api/leaderboard/boards/{boardId:D}/archive", null, ct: ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ScoringPropertyRecord>> ListPropertiesAsync(Guid boardId, CancellationToken ct = default)
        => await GetAsAsync<IReadOnlyList<ScoringPropertyRecord>>($"api/leaderboard/boards/{boardId:D}/properties", ct: ct).ConfigureAwait(false)
           ?? [];

    /// <inheritdoc />
    public Task<ScoringPropertyRecord> SavePropertyAsync(Guid boardId, ScoringPropertyRecord property, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(property);
        return PutAsAsync<ScoringPropertyRecord, ScoringPropertyRecord>($"api/leaderboard/boards/{boardId:D}/properties", property, ct: ct);
    }

    /// <inheritdoc />
    public Task DeletePropertyAsync(Guid boardId, Guid propertyId, CancellationToken ct = default)
        => DeleteAsAsync<object>($"api/leaderboard/boards/{boardId:D}/properties/{propertyId:D}", ct: ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<MemberRecord>> ListMembersAsync(Guid boardId, CancellationToken ct = default)
        => await GetAsAsync<IReadOnlyList<MemberRecord>>($"api/leaderboard/boards/{boardId:D}/members", ct: ct).ConfigureAwait(false)
           ?? [];

    /// <inheritdoc />
    public async Task<IReadOnlyList<StandingRecord>> GetStandingsAsync(Guid boardId, CancellationToken ct = default)
        => await GetAsAsync<IReadOnlyList<StandingRecord>>($"api/leaderboard/boards/{boardId:D}/standings", ct: ct).ConfigureAwait(false)
           ?? [];

    /// <inheritdoc />
    public Task<InviteCreatedResponse> CreateInviteAsync(Guid boardId, CreateInviteApiRequest request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        return PostAsAsync<CreateInviteApiRequest, InviteCreatedResponse>($"api/leaderboard/boards/{boardId:D}/invites", request, ct: ct);
    }

    /// <inheritdoc />
    public Task RevokeInviteAsync(Guid inviteId, CancellationToken ct = default)
        => PostExpectingNoContentAsync<object>($"api/leaderboard/invites/{inviteId:D}/revoke", null, ct: ct);

    /// <inheritdoc />
    public Task<byte[]> GetInviteQrAsync(Guid inviteId, CancellationToken ct = default)
        => GetFileAsync($"api/leaderboard/invites/{inviteId:D}/qr", ct: ct);

    /// <inheritdoc />
    public Task<MemberRecord> RedeemInviteAsync(string token, bool asGuest = false, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(token);
        return PostAsAsync<RedeemInviteRequest, MemberRecord>(
            "api/leaderboard/join", new() { Token = token }, before: asGuest ? ApplyGuestActor : null, ct: ct);
    }

    /// <inheritdoc />
    public Task<EntryRecord> CreateEntryAsync(Guid boardId, CreateEntryRequest request, bool asGuest = false, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        return PostAsAsync<CreateEntryRequest, EntryRecord>(
            $"api/leaderboard/boards/{boardId:D}/entries", request, before: asGuest ? ApplyGuestActor : null, ct: ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EntryRecord>> ListEntriesAsync(Guid boardId, CancellationToken ct = default)
        => await GetAsAsync<IReadOnlyList<EntryRecord>>($"api/leaderboard/boards/{boardId:D}/entries", ct: ct).ConfigureAwait(false)
           ?? [];

    /// <inheritdoc />
    public Task<EntryRecord?> GetEntryAsync(Guid entryId, CancellationToken ct = default)
        => GetAsAsync<EntryRecord>($"api/leaderboard/entries/{entryId:D}", ct: ct);

    /// <inheritdoc />
    public Task<EntryRecord> SetEntryStatusAsync(Guid entryId, EntryStatusChange change, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(change);
        return PostAsAsync<EntryStatusChange, EntryRecord>($"api/leaderboard/entries/{entryId:D}/status", change, ct: ct);
    }

    /// <inheritdoc />
    public Task<AdjustmentRecord> AddAdjustmentAsync(Guid boardId, CreateAdjustmentRequest request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        return PostAsAsync<CreateAdjustmentRequest, AdjustmentRecord>($"api/leaderboard/boards/{boardId:D}/adjustments", request, ct: ct);
    }

    /// <inheritdoc />
    public Task UndoAdjustmentAsync(Guid adjustmentId, CancellationToken ct = default)
        => PostExpectingNoContentAsync<object>($"api/leaderboard/adjustments/{adjustmentId:D}/undo", null, ct: ct);

    /// <inheritdoc />
    public Task RecalculateStandingsAsync(Guid boardId, CancellationToken ct = default)
        => PostExpectingNoContentAsync<object>($"api/leaderboard/boards/{boardId:D}/standings/recalculate", null, ct: ct);

    private static void ApplyGuestActor(HttpRequestMessage request)
        => request.Headers.TryAddWithoutValidation(LeaderboardAnonymousActor.HeaderName, LeaderboardAnonymousActor.Guest);
}

/// <summary>DI registration for <see cref="ILeaderboardApiClient" />.</summary>
public static class LeaderboardApiHttpClientRegistration
{
    /// <summary>Registers the typed Leaderboard API client from configuration.</summary>
    public static IHttpClientBuilder AddLeaderboardApiClientFromConfiguration(
        this IServiceCollection services,
        IConfiguration configuration,
        string configSectionName = LeaderboardApiClientOptions.SectionName)
    {
        ArgumentHelpers.ThrowIfNull(services);
        ArgumentHelpers.ThrowIfNull(configuration);
        var options = new LeaderboardApiClientOptions();
        configuration.GetSection(configSectionName).Bind(options);
        services.TryAddSingleton(Options.Create(options));
        return services.AddHttpClient<ILeaderboardApiClient, LeaderboardApiClient>(client => {
            if (!string.IsNullOrWhiteSpace(options.BaseUrl))
                client.BaseAddress = new(options.BaseUrl!.TrimEnd('/') + "/");

            LyoHttpClient.ApplyAcceptEncodingHeaders(client, options.AcceptEncodings);
        }).UseLyoHttpClientHandler<LeaderboardApiClientOptions>();
    }
}
