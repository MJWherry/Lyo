namespace Lyo.Billing;

/// <summary>Invoice lifecycle. Issued invoices snapshot bill-to; later CRM edits do not rewrite them.</summary>
public enum InvoiceStatus
{
    /// <summary>Editable. Charges are not yet marked billed.</summary>
    Draft = 0,

    /// <summary>Numbered, frozen, AR posted.</summary>
    Issued = 1,

    /// <summary>Some but not all of the total has been paid.</summary>
    PartiallyPaid = 2,

    /// <summary>Amount paid covers the total.</summary>
    Paid = 3,

    /// <summary>Unpaid issue was reversed. Charges are unlinked.</summary>
    Void = 4
}