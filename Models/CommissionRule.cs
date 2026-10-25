namespace SmartErpDesktop.Models;

/// <summary>
/// A representative's configurable percentage rule. Empty product and sale type
/// values mean that dimension is unrestricted. This record stores a rule only;
/// it does not create or accrue a commission.
/// </summary>
public sealed class CommissionRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string RepresentativeCode { get; set; } = "";
    public string Description { get; set; } = "";
    public string ProductCode { get; set; } = "";
    public string SaleType { get; set; } = "";
    public decimal RatePercent { get; set; }
    public CommissionBasis Basis { get; set; } = CommissionBasis.LineValueAfterLineDiscount;
    public int Priority { get; set; } = 100;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Supported value available on each invoice line after its own line discount,
/// before any invoice-wide discount. Further discount allocation is not modeled
/// by current invoices, so this basis is intentionally explicit.
/// </summary>
public enum CommissionBasis
{
    LineValueAfterLineDiscount = 0
}
