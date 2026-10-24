
namespace SmartErpDesktop.Models;

public sealed class Product
{
    public string Code { get; set; } = "";
    public string Barcode { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    public decimal Cost { get; set; }
    public decimal Stock { get; set; }
    public decimal MinimumStock { get; set; }
}

public sealed class Customer
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public decimal Balance { get; set; }
    public string Type { get; set; } = "فرد";
    public bool IsActive { get; set; } = true;
}

public sealed class InvoiceLine
{
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal? CostAtSale { get; set; }

    public decimal Total => Math.Max(0, Quantity * UnitPrice - Discount);
}

public sealed class Invoice
{
    public string Number { get; set; } = "";

    // تاريخ إنشاء الفاتورة، لا يتغير بعد الإنشاء.
    public DateTime Date { get; init; } = DateTime.Now;

    // تاريخ آخر تعديل، ويظل null لو الفاتورة لم تُعدّل.
    public DateTime? UpdatedAt { get; private set; }

    public string CustomerName { get; set; } = "عميل نقدي";
    public string CustomerCode { get; set; } = "";
    public string Type { get; set; } = "كاش";
    public decimal Total { get; set; }
    public decimal DiscountPercent { get; set; }
    public int InstallmentMonths { get; set; }
    public decimal? InitialPayment { get; set; }
    public decimal Paid { get; set; }
    public decimal Remaining => Total - Paid;
    public string Status { get; set; } = "مكتملة";
    public List<InvoiceLine> Lines { get; set; } = new();

    public void MarkUpdated()
    {
        UpdatedAt = DateTime.Now;
    }
}

public sealed class Payment
{
    public string ReceiptNumber { get; set; } = "";

    // تاريخ إنشاء إيصال التحصيل.
    public DateTime Date { get; init; } = DateTime.Now;

    // تاريخ آخر تعديل على بيانات الإيصال.
    public DateTime? UpdatedAt { get; private set; }

    public string CustomerName { get; set; } = "";
    public decimal Amount { get; set; }
    public string Method { get; set; } = "نقدي";
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Note { get; set; } = "";

    public void MarkUpdated()
    {
        UpdatedAt = DateTime.Now;
    }
}

public sealed class Supplier
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public decimal Balance { get; set; }
    public string Address { get; set; } = "";
    public string Notes { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public sealed class PurchaseLine
{
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public decimal Quantity { get; set; } = 1;
    public decimal UnitCost { get; set; }
    public decimal Discount { get; set; }
    public decimal Total => Math.Max(0, Quantity * UnitCost - Discount);
}

public sealed class SupplierPayment
{
    public string ReceiptNumber { get; set; } = "";
    public DateTime Date { get; set; } = DateTime.Now;
    public string SupplierName { get; set; } = "";
    public string PurchaseNumber { get; set; } = "";
    public decimal Amount { get; set; }
    public string Method { get; set; } = "نقدي";
    public string Note { get; set; } = "";
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string InstallmentId { get; set; } = "";
}

public sealed class SupplierInstallment
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string PurchaseNumber { get; set; } = "";
    public string SupplierName { get; set; } = "";
    public int Sequence { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public DateTime? PaidDate { get; set; }
    public decimal Remaining => Math.Max(0, Amount - PaidAmount);
    public string Status => Remaining <= 0 ? "مسدد" : PaidAmount > 0 ? "مسدد جزئياً" : "غير مدفوع";
}

public sealed class Purchase
{
    public string Number { get; set; } = "";

    // تاريخ إنشاء عملية الشراء.
    public DateTime Date { get; init; } = DateTime.Now;

    // تاريخ آخر تعديل على عملية الشراء.
    public DateTime? UpdatedAt { get; private set; }

    public string Supplier { get; set; } = "";
    public int Lines { get; set; }
    public decimal Total { get; set; }
    public decimal Paid { get; set; }
    public decimal Remaining => Math.Max(0, Total - Paid);
    public bool IsReceived { get; set; }
    public List<PurchaseLine> Details { get; set; } = new();
    public string Status { get; set; } = "مسودة تجريبية";
    public int InstallmentMonths { get; set; }
    public decimal InitialPayment { get; set; }
    public DateTime? FirstInstallmentDueDate { get; set; }
    public decimal InvoiceDiscount { get; set; }

    public void MarkUpdated()
    {
        UpdatedAt = DateTime.Now;
    }
}

public sealed class StockMovement
{
    // تاريخ تسجيل حركة المخزون.
    public DateTime Date { get; init; } = DateTime.Now;

    // تاريخ آخر تعديل على بيانات الحركة.
    public DateTime? UpdatedAt { get; private set; }

    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string Type { get; set; } = "";
    public decimal Quantity { get; set; }
    public string Reason { get; set; } = "";
    public string User { get; set; } = "موظف تجريبي";

    public void MarkUpdated()
    {
        UpdatedAt = DateTime.Now;
    }
}
public sealed class Expense
{
    // رقم المصروف
    public string Number { get; set; } = "";

    // تاريخ ووقت تسجيل المصروف
    public DateTime Date { get; init; } = DateTime.Now;

    // تصنيف المصروف
    public string Category { get; set; } = "أخرى";

    // وصف المصروف
    public string Description { get; set; } = "";

    // المبلغ
    public decimal Amount { get; set; }

    // طريقة الدفع
    public string PaymentMethod { get; set; } = "نقدي";

    // اسم الموظف الذي سجّل المصروف
    public string CreatedBy { get; set; } = "موظف تجريبي";

    // ملاحظات إضافية
    public string Notes { get; set; } = "";
}
