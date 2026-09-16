using Lyo.EntityReference.Models;

namespace Lyo.Billing;

/// <summary>
/// Invoices and charges. Issue, void, and pay post journals through <c>ILedgerStore</c>. There is no PDF in this plugin.
/// </summary>
/// <remarks>Pass null for <c>tenantId</c> on single-tenant hosts. Copied tax ids are encrypted on the invoice row.</remarks>
public interface IBillingStore
{
    /// <summary>Inserts or updates a bill-to client. Generates <see cref="BillingClientRecord.Id" /> when empty. Encrypts the tax id when one is supplied.</summary>
    Task SaveClientAsync(BillingClientRecord client, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Loads one bill-to client. Decrypts the tax id into <see cref="BillingClientRecord.TaxId" />.</summary>
    Task<BillingClientRecord?> GetClientByIdAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Inserts or updates an unbilled charge. Generates <see cref="ChargeRecord.Id" /> when empty.</summary>
    Task SaveChargeAsync(ChargeRecord charge, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Loads one charge.</summary>
    Task<ChargeRecord?> GetChargeByIdAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Unbilled charges for a client (<see cref="ChargeRecord.InvoiceId" /> is null).</summary>
    Task<IReadOnlyList<ChargeRecord>> ListUnbilledChargesAsync(Guid clientId, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Creates a draft invoice whose lines copy the given charges. Totals include tax unless the client is tax-exempt. Reserves each charge on the draft.</summary>
    Task<InvoiceRecord> CreateInvoiceFromChargesAsync(Guid clientId, IReadOnlyList<Guid> chargeIds, decimal taxRate = 0m, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Creates an empty draft invoice for a client. Add lines with <see cref="AddInvoiceLineAsync" /> before issuing.</summary>
    Task<InvoiceRecord> CreateDraftInvoiceAsync(Guid clientId, decimal taxRate = 0m, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>
    /// Adds a line to a draft. When <see cref="InvoiceLineRecord.ChargeId" /> is set, copies missing description/qty/rate from the charge and reserves it.
    /// </summary>
    Task<InvoiceLineRecord> AddInvoiceLineAsync(Guid invoiceId, InvoiceLineRecord line, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Removes a draft line and unreserves its charge when one was reserved.</summary>
    Task RemoveInvoiceLineAsync(Guid invoiceId, Guid lineId, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Deletes a draft and unreserves every charge on it. Fails when the invoice is not a draft.</summary>
    Task DeleteDraftInvoiceAsync(Guid invoiceId, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Counts and totals for the billing dashboard overview.</summary>
    Task<BillingSummaryRecord> GetBillingSummaryAsync(Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Loads an invoice and its lines. Decrypts the tax snapshot into <see cref="InvoiceRecord.TaxId" />.</summary>
    Task<InvoiceRecord?> GetInvoiceByIdAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>
    /// Issues a draft: assigns a number, snapshots bill-to (including encrypted tax id), freezes lines, marks charges billed, posts Dr AR / Cr Revenue / Cr Tax.
    /// </summary>
    Task<InvoiceRecord> IssueInvoiceAsync(Guid invoiceId, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Reverses the issue journal, unlinks charges, and sets status Void. Fails when any payment has been recorded.</summary>
    Task<InvoiceRecord> VoidInvoiceAsync(Guid invoiceId, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Records a payment and posts Dr Cash / Cr AR. Updates amount paid and status. Pass null <paramref name="currency" /> to use the invoice currency.</summary>
    Task<PaymentRecord> RecordPaymentAsync(Guid invoiceId, decimal amount, string? currency, PaymentMethod method, string? reference, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Loads one payment.</summary>
    Task<PaymentRecord?> GetPaymentByIdAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Inserts or updates the tenant biller profile. Encrypts bank/remittance secrets.</summary>
    Task SaveBillerProfileAsync(BillerProfileRecord profile, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Loads the biller profile for the tenant, decrypting secrets.</summary>
    Task<BillerProfileRecord?> GetBillerProfileAsync(Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Inserts or updates a lite person payload.</summary>
    Task<BillingPersonRecord> SavePersonAsync(BillingPersonRecord person, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Loads a lite person payload.</summary>
    Task<BillingPersonRecord?> GetPersonByIdAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Inserts or updates a lite phone payload.</summary>
    Task<BillingPhoneNumberRecord> SavePhoneNumberAsync(BillingPhoneNumberRecord phone, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Loads a lite phone payload.</summary>
    Task<BillingPhoneNumberRecord?> GetPhoneNumberByIdAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Inserts or updates a lite email payload.</summary>
    Task<BillingEmailAddressRecord> SaveEmailAddressAsync(BillingEmailAddressRecord email, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Loads a lite email payload.</summary>
    Task<BillingEmailAddressRecord?> GetEmailAddressByIdAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Inserts or updates a lite address payload.</summary>
    Task<BillingAddressRecord> SaveAddressAsync(BillingAddressRecord address, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Loads a lite address payload.</summary>
    Task<BillingAddressRecord?> GetAddressByIdAsync(Guid id, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Attaches a party to a billing client. Idempotent when the same contact EntityRef is already linked.</summary>
    Task<BillingContactRecord> AttachContactAsync(
        Guid clientId,
        EntityRef contact,
        BillingContactRole role = BillingContactRole.Other,
        string? title = null,
        bool isPrimary = false,
        bool isBillingContact = false,
        Guid? tenantId = null,
        CancellationToken ct = default);

    /// <summary>Soft-deletes a contact link.</summary>
    Task DetachContactAsync(Guid linkId, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Lists current contact links for a billing client.</summary>
    Task<IReadOnlyList<BillingContactRecord>> ListContactsAsync(Guid clientId, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Primary billing contact link, if any.</summary>
    Task<BillingContactRecord?> GetPrimaryBillingContactAsync(Guid clientId, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Attaches a phone payload to a billing client.</summary>
    Task<BillingPhoneLinkRecord> AttachPhoneAsync(
        Guid clientId,
        EntityRef phone,
        BillingPhoneUsage usage = BillingPhoneUsage.Other,
        bool isPrimary = false,
        EntityRef? contact = null,
        Guid? tenantId = null,
        CancellationToken ct = default);

    /// <summary>Soft-deletes a phone link.</summary>
    Task DetachPhoneAsync(Guid linkId, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Lists current phone links for a billing client.</summary>
    Task<IReadOnlyList<BillingPhoneLinkRecord>> ListPhonesAsync(Guid clientId, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Attaches an email payload to a billing client.</summary>
    Task<BillingEmailLinkRecord> AttachEmailAsync(
        Guid clientId,
        EntityRef email,
        BillingEmailUsage usage = BillingEmailUsage.Other,
        bool isPrimary = false,
        EntityRef? contact = null,
        Guid? tenantId = null,
        CancellationToken ct = default);

    /// <summary>Soft-deletes an email link.</summary>
    Task DetachEmailAsync(Guid linkId, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Lists current email links for a billing client.</summary>
    Task<IReadOnlyList<BillingEmailLinkRecord>> ListEmailsAsync(Guid clientId, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Attaches an address payload to a billing client.</summary>
    Task<BillingAddressLinkRecord> AttachAddressAsync(
        Guid clientId,
        EntityRef address,
        BillingAddressUsage usage = BillingAddressUsage.Other,
        bool isPrimary = false,
        EntityRef? contact = null,
        Guid? tenantId = null,
        CancellationToken ct = default);

    /// <summary>Soft-deletes an address link.</summary>
    Task DetachAddressAsync(Guid linkId, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Lists current address links for a billing client.</summary>
    Task<IReadOnlyList<BillingAddressLinkRecord>> ListAddressesAsync(Guid clientId, Guid? tenantId = null, CancellationToken ct = default);

    /// <summary>Primary billing address link, if any.</summary>
    Task<BillingAddressLinkRecord?> GetPrimaryBillingAddressAsync(Guid clientId, Guid? tenantId = null, CancellationToken ct = default);
}
