using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lyo.Authentication.Audit;
using Lyo.Authentication.Models.Audit;
using Lyo.Authentication.Models.Records;
using Lyo.Authentication.Postgres.Database;
using Lyo.Authentication.Postgres.Stores;
using Lyo.EntityReference.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Authentication.Postgres.Tests;

public sealed class PostgresAuthAuditRecorderTests
{
    private readonly AuthenticationPostgresFixture _fixture;

    public PostgresAuthAuditRecorderTests(AuthenticationPostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public void Recorder_IsResolvedFromDi_AsPostgresImpl()
    {
        Assert.IsType<PostgresAuthAuditRecorder>(_fixture.AuthAuditRecorder);
    }

    [Fact]
    public async Task RecordAsync_PersistsRow()
    {
        var recorder = NewRecorder();
        var evt = NewEvent(AuthAuditEventKind.JwtIssued, subject: "jti-" + Guid.NewGuid().ToString("N"));
        await recorder.RecordAsync(evt, TestContext.Current.CancellationToken);

        await using var ctx = await _fixture.ContextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var row = await ctx.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == evt.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(row);
        Assert.Equal(evt.Kind, row!.Kind);
        Assert.Equal(evt.Subject, row.Subject);
    }

    [Fact]
    public async Task RecordAsync_KindIsStoredAsEnumName()
    {
        var recorder = NewRecorder();
        var evt = NewEvent(AuthAuditEventKind.HandoffCodeIssued);
        await recorder.RecordAsync(evt, TestContext.Current.CancellationToken);

        await using var ctx = await _fixture.ContextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var conn = ctx.Database.GetDbConnection();
        await conn.OpenAsync(TestContext.Current.CancellationToken);
        try {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT kind FROM \"user\".event WHERE id = @id";
            var p = cmd.CreateParameter();
            p.ParameterName = "id";
            p.Value = evt.Id;
            cmd.Parameters.Add(p);
            var raw = (string?)await cmd.ExecuteScalarAsync(TestContext.Current.CancellationToken);
            Assert.Equal("HandoffCodeIssued", raw);
        }
        finally {
            await conn.CloseAsync();
        }
    }

    [Fact]
    public async Task RecordAsync_PersistsMetadataAsJsonb()
    {
        var recorder = NewRecorder();
        var metadata = new Dictionary<string, object?> {
            ["origin"] = "https://gateway.example",
            ["count"] = 3
        };
        var evt = NewEvent(AuthAuditEventKind.HandoffCodeConsumed, metadata: metadata);
        await recorder.RecordAsync(evt, TestContext.Current.CancellationToken);

        await using var ctx = await _fixture.ContextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var row = await ctx.Events.AsNoTracking().FirstAsync(e => e.Id == evt.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(row.MetadataJson);
        Assert.Contains("origin", row.MetadataJson);
        Assert.Contains("gateway.example", row.MetadataJson);
    }

    [Fact]
    public async Task RecordAsync_WithUserId_SetsForeignKey()
    {
        var user = await CreateUserAsync();
        var recorder = NewRecorder();
        var evt = NewEvent(AuthAuditEventKind.ExternalLoginSucceeded, userId: user.Id, provider: "google");
        await recorder.RecordAsync(evt, TestContext.Current.CancellationToken);

        await using var ctx = await _fixture.ContextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var row = await ctx.Events.AsNoTracking().FirstAsync(e => e.Id == evt.Id, TestContext.Current.CancellationToken);
        Assert.Equal(user.Id, row.UserId);
        Assert.Equal("google", row.Provider);
    }

    [Fact]
    public async Task RecordAsync_WithNullMetadata_StoresNull()
    {
        var recorder = NewRecorder();
        var evt = NewEvent(AuthAuditEventKind.TokenRejected, metadata: null);
        await recorder.RecordAsync(evt, TestContext.Current.CancellationToken);

        await using var ctx = await _fixture.ContextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var row = await ctx.Events.AsNoTracking().FirstAsync(e => e.Id == evt.Id, TestContext.Current.CancellationToken);
        Assert.Null(row.MetadataJson);
    }

    [Fact]
    public async Task RecordAsync_AcceptsEnrichmentFromContext()
    {
        var recorder = NewRecorder();
        var ctxAccessor = new FakeAccessor("203.0.113.7", "ua/postgres-test", "trace-" + Guid.NewGuid().ToString("N"));

        await recorder.RecordAsync(ctxAccessor, NullLogger.Instance, AuthAuditEventKind.SignedOut, outcome: "success", reason: "user_initiated");

        await using var ctx = await _fixture.ContextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var row = await ctx.Events.AsNoTracking()
            .Where(e => e.CorrelationId == ctxAccessor.CorrelationId)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(row);
        Assert.Equal("203.0.113.7", row!.IpAddress);
        Assert.Equal("ua/postgres-test", row.UserAgent);
        Assert.Equal("success", row.Outcome);
        Assert.Equal("user_initiated", row.Reason);
    }

    [Fact]
    public async Task RecordAsync_WithTenantId_PersistsTenantOnRow()
    {
        var recorder = NewRecorder();
        var tenantId = Guid.NewGuid();
        var evt = NewEvent(AuthAuditEventKind.JwtIssued, tenantId: tenantId);
        await recorder.RecordAsync(evt, TestContext.Current.CancellationToken);

        await using var ctx = await _fixture.ContextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var row = await ctx.Events.AsNoTracking().FirstAsync(e => e.Id == evt.Id, TestContext.Current.CancellationToken);
        Assert.Equal(tenantId, row.TenantId);
    }

    [Fact]
    public async Task RecordAsync_SystemOnlyMode_PersistsNullTenant()
    {
        var recorder = new PostgresAuthAuditRecorder(
            _fixture.ContextFactory,
            NullLogger<PostgresAuthAuditRecorder>.Instance,
            Microsoft.Extensions.Options.Options.Create(new EntityRefOptions()),
            Microsoft.Extensions.Options.Options.Create(new PostgresUserOptions { Tenancy = new TenancyOptions { Mode = TenancyMode.SystemOnly } }));
        var evt = NewEvent(AuthAuditEventKind.JwtIssued, tenantId: Guid.NewGuid());
        await recorder.RecordAsync(evt, TestContext.Current.CancellationToken);

        await using var ctx = await _fixture.ContextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var row = await ctx.Events.AsNoTracking().FirstAsync(e => e.Id == evt.Id, TestContext.Current.CancellationToken);
        Assert.Null(row.TenantId);
    }

    [Fact]
    public async Task RecordAsync_MultiTenantStrictMode_SwallowsErrorWhenCallerOmitsTenant()
    {
        var recorder = new PostgresAuthAuditRecorder(
            _fixture.ContextFactory,
            NullLogger<PostgresAuthAuditRecorder>.Instance,
            Microsoft.Extensions.Options.Options.Create(new EntityRefOptions()),
            Microsoft.Extensions.Options.Options.Create(new PostgresUserOptions { Tenancy = new TenancyOptions { Mode = TenancyMode.MultiTenantStrict } }));
        var evt = NewEvent(AuthAuditEventKind.JwtIssued);
        await recorder.RecordAsync(evt, TestContext.Current.CancellationToken);

        await using var ctx = await _fixture.ContextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var row = await ctx.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == evt.Id, TestContext.Current.CancellationToken);
        Assert.Null(row);
    }

    [Fact]
    public async Task UserSchema_EventTable_Exists()
    {
        await using var ctx = await _fixture.ContextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var conn = ctx.Database.GetDbConnection();
        await conn.OpenAsync(TestContext.Current.CancellationToken);
        try {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'user' AND table_name = 'event'";
            var count = (long)(await cmd.ExecuteScalarAsync(TestContext.Current.CancellationToken))!;
            Assert.Equal(1L, count);
        }
        finally {
            await conn.CloseAsync();
        }
    }

    private PostgresAuthAuditRecorder NewRecorder() =>
        new(_fixture.ContextFactory,
            NullLogger<PostgresAuthAuditRecorder>.Instance,
            Microsoft.Extensions.Options.Options.Create(new EntityRefOptions()),
            Microsoft.Extensions.Options.Options.Create(new PostgresUserOptions()));

    private static AuthAuditEvent NewEvent(
        AuthAuditEventKind kind,
        Guid? userId = null,
        string? subject = null,
        string? provider = null,
        Guid? tenantId = null,
        IReadOnlyDictionary<string, object?>? metadata = null) =>
        new(
            Id: Guid.NewGuid(),
            Timestamp: DateTime.UtcNow,
            Kind: kind,
            UserId: userId,
            Subject: subject,
            Provider: provider,
            Outcome: "success",
            Reason: null,
            IpAddress: "127.0.0.1",
            UserAgent: "test-runner",
            CorrelationId: "test-" + Guid.NewGuid().ToString("N"),
            TenantId: tenantId,
            Metadata: metadata);

    private async Task<LyoUser> CreateUserAsync()
    {
        var user = new LyoUser(
            Id: Guid.NewGuid(),
            DisplayName: "Audit User",
            Email: $"audit-{Guid.NewGuid():N}@example.com",
            EmailVerified: true,
            AvatarUrl: null,
            PreferredLanguageBcp47: null,
            Scopes: [],
            Metadata: null,
            PersonId: null,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: null,
            LastLoginAt: null,
            DisabledAt: null,
            DisabledReason: null);
        return await _fixture.UserStore.CreateAsync(user, tenantId: null, TestContext.Current.CancellationToken);
    }

    private sealed class FakeAccessor(string ip, string ua, string corr) : IAuthAuditContextAccessor
    {
        public string? IpAddress { get; } = ip;
        public string? UserAgent { get; } = ua;
        public string? CorrelationId { get; } = corr;
    }
}
