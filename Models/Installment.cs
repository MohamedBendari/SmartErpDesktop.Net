namespace SmartErpDesktop.Models;

/// <summary>دفعة مجدولة مرتبطة بفاتورة بيع بالتقسيط.</summary>
public sealed class Installment
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string InvoiceNumber { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public int Sequence { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Remaining => Math.Max(0, Amount - PaidAmount);
    public string Status => Remaining <= 0 ? "مسدد" : DueDate.Date < DateTime.Today ? "متأخر" : PaidAmount > 0 ? "مسدد جزئياً" : "مستحق لاحقاً";
}
