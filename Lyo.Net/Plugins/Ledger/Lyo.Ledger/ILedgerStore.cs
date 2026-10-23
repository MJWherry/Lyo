namespace Lyo.Ledger;

/// <summary>Double-entry books. Billing must not change a balance except by posting here. Listing and search belong in QueryProject.</summary>
/// <remarks>Pass null for <c>tenantId</c> on single-tenant hosts. There is no PostgreSQL FK to <c>client</c>; <c>client_id</c> on lines is a Guid.</remarks>
public interface ILedgerStore
{
    /// <summary>Inserts the seeded CoA codes (1000/1100/2200/4000) when missing for this tenant.</summary>
    Task EnsureChartOfAccountsAsync(Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Inserts or updates an account. Generates <see cref="LedgerAccountRecord.Id" /> when empty.</summary>
    Task SaveAccountAsync(LedgerAccountRecord account, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Loads one account by id.</summary>
    Task<LedgerAccountRecord?> GetAccountByIdAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Loads one account by code (trimmed, case-insensitive).</summary>
    Task<LedgerAccountRecord?> GetAccountByCodeAsync(string code, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Lists accounts for the tenant.</summary>
    Task<IReadOnlyList<LedgerAccountRecord>> ListAccountsAsync(Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Creates a draft journal with lines. Does not post.</summary>
    Task<LedgerJournalRecord> SaveDraftJournalAsync(
        LedgerJournalRecord journal,
        IReadOnlyList<LedgerJournalLineRecord> lines,
        Guid? tenantId = null,
        CancellationToken ct = default);

    /// <summary>Loads a journal and its lines.</summary>
    Task<LedgerJournalRecord?> GetJournalByIdAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Creates (if needed) and posts a balanced journal. Seeded accounts are ensured first. Posted journals cannot be edited.</summary>
    Task<LedgerJournalRecord> PostJournalAsync(LedgerJournalRecord journal, IReadOnlyList<LedgerJournalLineRecord> lines, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Posts an existing draft. Fails when already posted or unbalanced.</summary>
    Task<LedgerJournalRecord> PostJournalAsync(Guid journalId, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Writes a reversing posted journal and marks the original voided. Never deletes posted rows.</summary>
    Task<LedgerJournalRecord> VoidJournalAsync(Guid journalId, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Posted AR (account 1100) debit minus credit for this client. Voided journals and reversing journals are excluded.</summary>
    Task<decimal> GetClientArBalanceAsync(Guid clientId, Guid? tenantId = null, CancellationToken ct = default);
}