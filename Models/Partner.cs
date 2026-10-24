namespace SmartErpDesktop.Models;

public sealed class Partner
{
    public string Code { get; set; } = "";
    public string FullName { get; set; } = "";
    public string NationalId { get; set; } = "";
    public string Mobile { get; set; } = "";
    public string AlternatePhone { get; set; } = "";
    public string Address { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime JoinDate { get; set; } = DateTime.Today;
    public decimal InitialCapital { get; set; }
    public DateTime InitialContributionDate { get; set; } = DateTime.Today;
    public decimal ProfitSharePercent { get; set; }
    public decimal LossSharePercent { get; set; }
    // Losses affect the capital settlement only when the partnership agreement explicitly says so.
    public bool LossesReduceCapitalAccount { get; set; }
    public decimal CapitalOwnershipPercent { get; set; }
    public string Notes { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime? ExitDate { get; set; }
}

public sealed class PartnerTransaction
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string PartnerCode { get; set; } = "";
    public string Type { get; set; } = ""; // Contribution, CapitalReturn, ProfitDistribution, ProfitAdjustment, Reversal
    public DateTime Date { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "نقدي";
    public string ReferenceNumber { get; set; } = "";
    public string Notes { get; set; } = "";
    public DateTime RecordedAt { get; set; } = DateTime.Now;
    public string ReversesTransactionId { get; set; } = "";
    public string SettlementId { get; set; } = "";
}

public sealed class PartnerProfitAllocation
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string PartnerCode { get; set; } = "";
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal BusinessNetProfit { get; set; }
    public decimal EffectiveSharePercent { get; set; }
    public decimal EffectiveLossSharePercent { get; set; }
    public bool LossReducesCapitalAccount { get; set; }
    public decimal AllocatedProfit { get; set; }
    public decimal AllocatedLoss { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.Now;
    public bool IsFinalized { get; set; }
    public string ReversesAllocationId { get; set; } = "";
}

public sealed class PartnerExitSettlement
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string PartnerCode { get; set; } = "";
    public DateTime ProposedExitDate { get; set; }
    public DateTime ConfirmedAt { get; set; } = DateTime.Now;
    public decimal RemainingCapital { get; set; }
    public decimal TotalCapitalContributed { get; set; }
    public decimal CapitalReturnedPreviously { get; set; }
    public decimal AccumulatedProfits { get; set; }
    public decimal AccumulatedLosses { get; set; }
    public decimal ProfitAlreadyPaid { get; set; }
    public decimal CapitalLossOffset { get; set; }
    public decimal UnpaidProfit { get; set; }
    public decimal ApprovedDeductions { get; set; }
    public decimal ProposedTotal { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal CapitalPaid { get; set; }
    public decimal ProfitPaid { get; set; }
    public decimal RemainingSettlement => Math.Max(0, ProposedTotal - AmountPaid);
    public string ReferenceNumber { get; set; } = "";
    public string Notes { get; set; } = "";
}
