namespace Lyo.Billing;

/// <summary>Tenant-wide billing KPIs for the dashboard overview.</summary>
public sealed class BillingSummaryRecord
{
    /// <summary>Draft invoices.</summary>
    public int DraftCount { get; set; }

    /// <summary>Issued and unpaid invoices.</summary>
    public int IssuedCount { get; set; }

    /// <summary>Partially paid invoices.</summary>
    public int PartiallyPaidCount { get; set; }

    /// <summary>Fully paid invoices.</summary>
    public int PaidCount { get; set; }

    /// <summary>Void invoices.</summary>
    public int VoidCount { get; set; }

    /// <summary>Remaining AR on issued and partially paid invoices (total − amount paid).</summary>
    public decimal OutstandingTotal { get; set; }

    /// <summary>Sum of recorded payments on non-void invoices.</summary>
    public decimal PaidTotal { get; set; }

    /// <summary>Unbilled charges (no reserved invoice).</summary>
    public int UnbilledChargeCount { get; set; }

    /// <summary>Sum of unbilled charge amounts.</summary>
    public decimal UnbilledChargeTotal { get; set; }
}