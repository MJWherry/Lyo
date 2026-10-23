using System.Transactions;
using Lyo.Common.Core.Identifiers;
using Lyo.EntityReference.Models;
using Lyo.EntityReference.Postgres;
using Lyo.Exceptions;
using Lyo.Health;
using Lyo.Ledger.Postgres.Database;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;

namespace Lyo.Ledger.Postgres;

/// <summary>Postgres-backed <see cref="ILedgerStore" />.</summary>
public sealed class PostgresLedgerStore : ILedgerStore, IHealth
{
    private static readonly (string Code, string Name, LedgerAccountType Type)[] SeedAccounts = [
        (LedgerAccountCodes.Cash, "Cash", LedgerAccountType.Asset), (LedgerAccountCodes.AccountsReceivable, "Accounts receivable", LedgerAccountType.Asset),
        (LedgerAccountCodes.TaxPayable, "Tax payable", LedgerAccountType.Liability), (LedgerAccountCodes.Revenue, "Revenue", LedgerAccountType.Revenue)
    ];

    private readonly IDbContextFactory<LedgerDbContext> _contextFactory;
    private readonly EntityRefOptions _entityRefOptions;
    private readonly TenancyOptions _featureTenancy;

    /// <summary>Constructs the store.</summary>
    public PostgresLedgerStore(IDbContextFactory<LedgerDbContext> contextFactory, EntityRefOptions entityRefOptions, PostgresLedgerOptions ledgerOptions)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory);
        ArgumentHelpers.ThrowIfNull(entityRefOptions);
        ArgumentHelpers.ThrowIfNull(ledgerOptions);
        _contextFactory = contextFactory;
        _entityRefOptions = entityRefOptions;
        _featureTenancy = ledgerOptions.Tenancy;
    }

    /// <inheritdoc />
    public string HealthCheckName => "ledger-postgres";

    /// <inheritdoc />
    public Task<HealthResult> CheckHealthAsync(CancellationToken ct = default) => PostgresHealth.CheckAsync(_contextFactory, PostgresLedgerOptions.Schema, ct);

    /// <inheritdoc />
    public async Task EnsureChartOfAccountsAsync(Guid? tenantId = null, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        using var scope = new TransactionScope(
            TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted, Timeout = TransactionManager.DefaultTimeout },
            TransactionScopeAsyncFlowOption.Enabled);

        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await context.Database.OpenConnectionAsync(ct).ConfigureAwait(false);
        await context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(87231402)", ct).ConfigureAwait(false);
        await EnsureChartOfAccountsAsync(context, resolvedTenant, ct).ConfigureAwait(false);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        scope.Complete();
    }

    /// <inheritdoc />
    public async Task SaveAccountAsync(LedgerAccountRecord account, Guid? tenantId = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(account);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(account.Code, nameof(account.Code));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(account.Name, nameof(account.Name));
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        LedgerAccountEntity entity;
        if (account.Id != Guid.Empty) {
            var existing = await context.Accounts.FirstOrDefaultAsync(a => a.Id == account.Id && a.TenantId == resolvedTenant, ct).ConfigureAwait(false);
            entity = existing ?? new LedgerAccountEntity { Id = account.Id, CreatedAt = DateTime.UtcNow };
            if (existing == null)
                context.Accounts.Add(entity);
        }
        else {
            entity = new() { Id = LyoGuid.CreateV7(), CreatedAt = DateTime.UtcNow };
            context.Accounts.Add(entity);
            account.Id = entity.Id;
        }

        entity.TenantId = resolvedTenant;
        entity.Code = account.Code.Trim();
        entity.Name = account.Name.Trim();
        entity.AccountType = (int)account.AccountType;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        account.TenantId = entity.TenantId;
        account.CreatedAt = entity.CreatedAt;
    }

    /// <inheritdoc />
    public async Task<LedgerAccountRecord?> GetAccountByIdAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id && a.TenantId == resolvedTenant, ct).ConfigureAwait(false);
        return entity == null ? null : ToAccount(entity);
    }

    /// <inheritdoc />
    public async Task<LedgerAccountRecord?> GetAccountByCodeAsync(string code, Guid? tenantId = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(code);
        var resolvedTenant = ResolveTenant(tenantId);
        var lower = code.Trim().ToLowerInvariant();
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.TenantId == resolvedTenant && a.Code.ToLower() == lower, ct).ConfigureAwait(false);
        return entity == null ? null : ToAccount(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LedgerAccountRecord>> ListAccountsAsync(Guid? tenantId = null, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rows = await context.Accounts.AsNoTracking().Where(a => a.TenantId == resolvedTenant).OrderBy(a => a.Code).ToListAsync(ct).ConfigureAwait(false);
        return rows.Select(ToAccount).ToList();
    }

    /// <inheritdoc />
    public async Task<LedgerJournalRecord> SaveDraftJournalAsync(
        LedgerJournalRecord journal,
        IReadOnlyList<LedgerJournalLineRecord> lines,
        Guid? tenantId = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(journal);
        ArgumentHelpers.ThrowIfNull(lines);
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await UpsertDraftAsync(context, journal, lines, resolvedTenant, ct).ConfigureAwait(false);
        entity.Status = (int)LedgerJournalStatus.Draft;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return await LoadJournalAsync(context, entity.Id, resolvedTenant, ct).ConfigureAwait(false) ?? throw new InvalidOperationException("Draft journal was not saved.");
    }

    /// <inheritdoc />
    public async Task<LedgerJournalRecord?> GetJournalByIdAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await LoadJournalAsync(context, id, resolvedTenant, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<LedgerJournalRecord> PostJournalAsync(
        LedgerJournalRecord journal,
        IReadOnlyList<LedgerJournalLineRecord> lines,
        Guid? tenantId = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(journal);
        ArgumentHelpers.ThrowIfNull(lines);
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await EnsureChartOfAccountsAsync(context, resolvedTenant, ct).ConfigureAwait(false);
        var entity = await UpsertDraftAsync(context, journal, lines, resolvedTenant, ct).ConfigureAwait(false);
        PostEntity(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return await LoadJournalAsync(context, entity.Id, resolvedTenant, ct).ConfigureAwait(false) ?? throw new InvalidOperationException("Posted journal was not saved.");
    }

    /// <inheritdoc />
    public async Task<LedgerJournalRecord> PostJournalAsync(Guid journalId, Guid? tenantId = null, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await context.Journals.Include(j => j.Lines).FirstOrDefaultAsync(j => j.Id == journalId && j.TenantId == resolvedTenant, ct).ConfigureAwait(false) ??
            throw new InvalidOperationException($"Journal '{journalId}' was not found.");

        PostEntity(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return await LoadJournalAsync(context, entity.Id, resolvedTenant, ct).ConfigureAwait(false) ?? throw new InvalidOperationException("Posted journal was not saved.");
    }

    /// <inheritdoc />
    public async Task<LedgerJournalRecord> VoidJournalAsync(Guid journalId, Guid? tenantId = null, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var original = await context.Journals.Include(j => j.Lines).FirstOrDefaultAsync(j => j.Id == journalId && j.TenantId == resolvedTenant, ct).ConfigureAwait(false) ??
            throw new InvalidOperationException($"Journal '{journalId}' was not found.");

        if (original.Status != (int)LedgerJournalStatus.Posted)
            throw new LedgerPostedJournalImmutableException("Only a posted journal can be voided.");

        var reversal = new LedgerJournalEntity {
            Id = LyoGuid.CreateV7(),
            TenantId = resolvedTenant,
            Memo = string.IsNullOrWhiteSpace(original.Memo) ? "Void" : $"Void: {original.Memo}",
            SourceEntityType = original.SourceEntityType,
            SourceEntityId = original.SourceEntityId,
            VoidOfJournalId = original.Id,
            CreatedAt = DateTime.UtcNow,
            Status = (int)LedgerJournalStatus.Draft
        };

        var n = 1;
        foreach (var line in original.Lines.OrderBy(l => l.LineNumber)) {
            reversal.Lines.Add(
                new() {
                    Id = LyoGuid.CreateV7(),
                    LineNumber = n++,
                    AccountId = line.AccountId,
                    Debit = line.Credit,
                    Credit = line.Debit,
                    ClientId = line.ClientId,
                    Memo = line.Memo
                });
        }

        PostEntity(reversal);
        original.Status = (int)LedgerJournalStatus.Voided;
        original.VoidedAt = DateTime.UtcNow;
        context.Journals.Add(reversal);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return await LoadJournalAsync(context, reversal.Id, resolvedTenant, ct).ConfigureAwait(false) ?? throw new InvalidOperationException("Reversing journal was not saved.");
    }

    /// <inheritdoc />
    public async Task<decimal> GetClientArBalanceAsync(Guid clientId, Guid? tenantId = null, CancellationToken ct = default)
    {
        var resolvedTenant = ResolveTenant(tenantId);
        await using var context = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var ar = await context.Accounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.TenantId == resolvedTenant && a.Code == LedgerAccountCodes.AccountsReceivable, ct)
            .ConfigureAwait(false);

        if (ar == null)
            return 0m;

        var posted = (int)LedgerJournalStatus.Posted;
        return await context.JournalLines.AsNoTracking()
            .Where(l => l.AccountId == ar.Id && l.ClientId == clientId && l.Journal.TenantId == resolvedTenant && l.Journal.Status == posted && l.Journal.VoidOfJournalId == null)
            .SumAsync(l => l.Debit - l.Credit, ct)
            .ConfigureAwait(false);
    }

    private Guid? ResolveTenant(Guid? tenantId) => TenancyResolver.Resolve(tenantId, _featureTenancy, _entityRefOptions);

    private static async Task EnsureChartOfAccountsAsync(LedgerDbContext context, Guid? tenantId, CancellationToken ct)
    {
        var existing = await context.Accounts.Where(a => a.TenantId == tenantId).Select(a => a.Code).ToListAsync(ct).ConfigureAwait(false);
        var known = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
        foreach (var (code, name, type) in SeedAccounts) {
            if (known.Contains(code))
                continue;

            context.Accounts.Add(
                new() {
                    Id = LyoGuid.CreateV7(),
                    TenantId = tenantId,
                    Code = code,
                    Name = name,
                    AccountType = (int)type,
                    CreatedAt = DateTime.UtcNow
                });

            known.Add(code);
        }
    }

    private static async Task<LedgerJournalEntity> UpsertDraftAsync(
        LedgerDbContext context,
        LedgerJournalRecord journal,
        IReadOnlyList<LedgerJournalLineRecord> lines,
        Guid? tenantId,
        CancellationToken ct)
    {
        LedgerJournalEntity entity;
        if (journal.Id != Guid.Empty) {
            var existing = await context.Journals.Include(j => j.Lines).FirstOrDefaultAsync(j => j.Id == journal.Id && j.TenantId == tenantId, ct).ConfigureAwait(false);
            if (existing != null) {
                if (existing.Status != (int)LedgerJournalStatus.Draft)
                    throw new LedgerPostedJournalImmutableException("Posted journals cannot be edited. Void them instead.");

                context.JournalLines.RemoveRange(existing.Lines);
                entity = existing;
            }
            else {
                entity = new() { Id = journal.Id, CreatedAt = DateTime.UtcNow };
                context.Journals.Add(entity);
            }
        }
        else {
            entity = new() { Id = LyoGuid.CreateV7(), CreatedAt = DateTime.UtcNow };
            context.Journals.Add(entity);
            journal.Id = entity.Id;
        }

        entity.TenantId = tenantId;
        entity.Memo = journal.Memo;
        entity.SourceEntityType = journal.Source?.EntityType;
        entity.SourceEntityId = journal.Source?.EntityId;
        entity.VoidOfJournalId = journal.VoidOfJournalId;
        entity.Status = (int)LedgerJournalStatus.Draft;
        await EnsureAccountsBelongToTenantAsync(context, lines, tenantId, ct).ConfigureAwait(false);
        var n = 1;
        foreach (var line in lines) {
            ValidateLine(line);
            entity.Lines.Add(
                new() {
                    Id = line.Id == Guid.Empty ? LyoGuid.CreateV7() : line.Id,
                    JournalId = entity.Id,
                    LineNumber = line.LineNumber > 0 ? line.LineNumber : n,
                    AccountId = line.AccountId,
                    Debit = line.Debit,
                    Credit = line.Credit,
                    ClientId = line.ClientId,
                    Memo = line.Memo
                });

            n++;
        }

        return entity;
    }

    private static void PostEntity(LedgerJournalEntity entity)
    {
        if (entity.Status == (int)LedgerJournalStatus.Posted)
            throw new LedgerPostedJournalImmutableException("Journal is already posted.");

        if (entity.Status == (int)LedgerJournalStatus.Voided)
            throw new LedgerPostedJournalImmutableException("Voided journals cannot be posted.");

        ValidateBalanced(entity.Lines);
        entity.Status = (int)LedgerJournalStatus.Posted;
        entity.PostedAt = DateTime.UtcNow;
    }

    private static async Task EnsureAccountsBelongToTenantAsync(LedgerDbContext context, IReadOnlyList<LedgerJournalLineRecord> lines, Guid? tenantId, CancellationToken ct)
    {
        var ids = lines.Select(l => l.AccountId).Where(id => id != Guid.Empty).Distinct().ToList();
        if (ids.Count == 0)
            return;

        var accounts = await context.Accounts.AsNoTracking().Where(a => ids.Contains(a.Id)).Select(a => new { a.Id, a.TenantId }).ToListAsync(ct).ConfigureAwait(false);
        if (accounts.Count != ids.Count)
            throw new LedgerAccountTenantException("A journal line references an account that does not exist.");

        if (accounts.Any(a => a.TenantId != tenantId))
            throw new LedgerAccountTenantException("A journal line references an account that does not belong to this tenant.");
    }

    private static void ValidateLine(LedgerJournalLineRecord line)
    {
        if (line.AccountId == Guid.Empty)
            throw new ArgumentException("Each journal line requires an account.", nameof(line));

        if (line.Debit < 0 || line.Credit < 0)
            throw new ArgumentException("Debit and credit must be non-negative.", nameof(line));

        if (line.Debit == 0 && line.Credit == 0)
            throw new ArgumentException("Each journal line must have a debit or a credit.", nameof(line));

        if (line.Debit > 0 && line.Credit > 0)
            throw new ArgumentException("A journal line cannot be both a debit and a credit.", nameof(line));
    }

    private static void ValidateBalanced(IEnumerable<LedgerJournalLineEntity> lines)
    {
        var list = lines.ToList();
        if (list.Count < 2)
            throw new LedgerUnbalancedJournalException("A journal must have at least two lines.");

        var debit = list.Sum(l => l.Debit);
        var credit = list.Sum(l => l.Credit);
        if (debit != credit)
            throw new LedgerUnbalancedJournalException($"Journal is unbalanced: debit {debit} credit {credit}.");

        if (debit == 0)
            throw new LedgerUnbalancedJournalException("A posted journal must have a non-zero total.");
    }

    private static async Task<LedgerJournalRecord?> LoadJournalAsync(LedgerDbContext context, Guid id, Guid? tenantId, CancellationToken ct)
    {
        var entity = await context.Journals.AsNoTracking().Include(j => j.Lines).FirstOrDefaultAsync(j => j.Id == id && j.TenantId == tenantId, ct).ConfigureAwait(false);
        return entity == null ? null : ToJournal(entity);
    }

    private static LedgerAccountRecord ToAccount(LedgerAccountEntity e)
        => new() {
            Id = e.Id,
            TenantId = e.TenantId,
            Code = e.Code,
            Name = e.Name,
            AccountType = (LedgerAccountType)e.AccountType,
            CreatedAt = e.CreatedAt
        };

    private static LedgerJournalRecord ToJournal(LedgerJournalEntity e)
        => new() {
            Id = e.Id,
            TenantId = e.TenantId,
            Status = (LedgerJournalStatus)e.Status,
            Memo = e.Memo,
            Source = e.SourceEntityType is { Length: > 0 } t && e.SourceEntityId is { Length: > 0 } i ? EntityRef.ForKey(t, i) : null,
            VoidOfJournalId = e.VoidOfJournalId,
            PostedAt = e.PostedAt,
            VoidedAt = e.VoidedAt,
            CreatedAt = e.CreatedAt,
            Lines = e.Lines.OrderBy(l => l.LineNumber).Select(ToLine).ToList()
        };

    private static LedgerJournalLineRecord ToLine(LedgerJournalLineEntity e)
        => new() {
            Id = e.Id,
            JournalId = e.JournalId,
            LineNumber = e.LineNumber,
            AccountId = e.AccountId,
            Debit = e.Debit,
            Credit = e.Credit,
            ClientId = e.ClientId,
            Memo = e.Memo
        };
}