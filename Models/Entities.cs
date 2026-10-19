
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
}

public sealed class InvoiceLine
{
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }

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
    public string Type { get; set; } = "كاش";
    public decimal Total { get; set; }
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
    public string Status { get; set; } = "مسودة تجريبية";

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
