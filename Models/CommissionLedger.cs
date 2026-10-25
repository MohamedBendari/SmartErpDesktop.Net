namespace SmartErpDesktop.Models;

/// <summary>Immutable snapshot of commission earned from one collected invoice allocation.</summary>
public sealed class CommissionAccrual
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string SourceAllocationId { get; init; } = "";
    public string PaymentReceiptNumber { get; init; } = "";
    public DateTime CollectedAt { get; init; }
    public string InvoiceNumber { get; init; } = "";
    public string InvoiceLineId { get; init; } = "";
    public string ProductCode { get; init; } = "";
    public string RepresentativeCode { get; init; } = "";
    public string RepresentativeName { get; init; } = "";
    public string RuleId { get; init; } = "";
    public string RuleDescription { get; init; } = "";
    public string RuleScopeSnapshot { get; init; } = "";
    public decimal RatePercent { get; init; }
    public CommissionBasis Basis { get; init; }
    public decimal InvoiceEligibleBase { get; init; }
    public decimal InvoiceTotal { get; init; }
    public decimal InvoiceDiscountPercent { get; init; }
    public decimal PaymentAllocationAmount { get; init; }
    public decimal CollectedEligibleAmount { get; init; }
    public decimal CommissionAmount { get; init; }
    public List<CommissionAccrualLineSnapshot> InvoiceLines { get; init; } = new();
}

public sealed class CommissionAccrualLineSnapshot
{
    public string InvoiceLineId { get; init; } = "";
    public string ProductCode { get; init; } = "";
    public string ProductName { get; init; } = "";
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineDiscount { get; init; }
    public decimal EligibleValue { get; init; }
}

/// <summary>Append-only approval event for an earned commission accrual.</summary>
public sealed class CommissionApproval
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string AccrualId { get; init; } = "";
    public DateTime Date { get; init; } = DateTime.Now;
    public string ApprovedBy { get; init; } = "";
}

/// <summary>Separate representative payout receipt; never a customer collection.</summary>
public sealed class RepresentativePayout
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string ReceiptNumber { get; init; } = "";
    public DateTime Date { get; init; } = DateTime.Now;
    public string RepresentativeCode { get; init; } = "";
    public string RepresentativeName { get; init; } = "";
    public decimal Amount { get; init; }
    public string Method { get; init; } = "";
    public string Notes { get; init; } = "";
    public List<CommissionPayoutAllocation> Allocations { get; init; } = new();
}

public sealed class CommissionPayoutAllocation
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string AccrualId { get; init; } = "";
    public decimal Amount { get; init; }
}

public enum CommissionAccrualStatus
{
    Earned,
    Approved,
    PartiallyPaid,
    Paid
}
