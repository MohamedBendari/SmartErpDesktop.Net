using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using SmartErpDesktop.Models;

namespace SmartErpDesktop.Views;

// ─────────────────────────────────────────────────────────────────────────────
// Shared enterprise-grade document factory used by all print/preview surfaces.
// Centralises layout so every invoice, statement and schedule looks identical
// whether the user hits "Details / Preview" or "Print Selected Record".
// ─────────────────────────────────────────────────────────────────────────────
internal static class ErpPrintDocument
{
    // ── Palette ───────────────────────────────────────────────────────────────
    private static readonly SolidColorBrush HeaderBg      = new(Color.FromRgb(30,  58,  95));  // navy
    private static readonly SolidColorBrush HeaderFg      = Brushes.White;
    private static readonly SolidColorBrush AltRowBg      = new(Color.FromRgb(245, 247, 250)); // light blue-grey
    private static readonly SolidColorBrush TotalsBg      = new(Color.FromRgb(232, 240, 254)); // pale blue
    private static readonly SolidColorBrush BorderBrush   = new(Color.FromRgb(176, 196, 222)); // steel blue
    private static readonly SolidColorBrush DividerBrush  = new(Color.FromRgb(180, 180, 180));
    private static readonly SolidColorBrush SubheadFg     = new(Color.FromRgb(30,  58,  95));
    private static readonly SolidColorBrush MutedFg       = new(Color.FromRgb(100, 100, 110));

    // ── Font sizes ────────────────────────────────────────────────────────────
    private const double TitleSize   = 22;
    private const double HeadingSize = 14;
    private const double BodySize    = 11;
    private const double SmallSize   = 9.5;

    // ═════════════════════════════════════════════════════════════════════════
    //  PUBLIC RECORD: used by CustomerStatementWindow to avoid private coupling
    // ═════════════════════════════════════════════════════════════════════════
    public sealed record StatementRow(
        DateTime Date, string Kind, string Number, string Description,
        decimal Amount, decimal Paid, decimal Balance, string Status);

    // ═════════════════════════════════════════════════════════════════════════
    //  DOCUMENT FACTORIES
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Professional A4 invoice — used for Details/Preview AND Print.</summary>
    internal static FlowDocument CreateInvoiceDocument(
        Invoice invoice, Customer? customer, CompanySettings settings,
        IReadOnlyList<Installment> installments)
    {
        bool thermal58 = settings.PaperSize.Contains("58");
        bool a4        = settings.PaperSize.Equals("A4", StringComparison.OrdinalIgnoreCase);
        double width   = a4 ? 793.7 : thermal58 ? 219.2 : 302.4;
        double fs      = a4 ? BodySize : thermal58 ? 8.5 : 9.5;

        var doc = MakeDocument(width, a4 ? 1122.5 : 900, a4 ? 36 : 10, fs);

        // ── Company header ────────────────────────────────────────────────────
        if (settings.ShowStoreInfo)
            AddCompanyHeader(doc, settings, fs, a4);

        // ── Invoice title ─────────────────────────────────────────────────────
        AddCentredPara(doc, "فاتورة مبيعات", a4 ? 18 : fs + 3, bold: true);
        AddDivider(doc, fs);

        // ── Invoice metadata (2-column info grid) ─────────────────────────────
        var meta = MetaTable(doc);
        AddMetaRow(meta, fs, "رقم الفاتورة", invoice.Number, "التاريخ", invoice.Date.ToString("yyyy/MM/dd HH:mm"));
        AddMetaRow(meta, fs, "نوع البيع",    invoice.Type,   "الحالة",   invoice.Status);

        if (customer is not null)
        {
            AddMetaRow(meta, fs, "كود العميل", customer.Code, "اسم العميل", customer.Name);
            if (!string.IsNullOrWhiteSpace(customer.Phone))
                AddMetaRow(meta, fs, "هاتف العميل", customer.Phone, "", "");
            if (!string.IsNullOrWhiteSpace(customer.Address))
                AddMetaRow(meta, fs, "العنوان", customer.Address, "", "");
        }
        else if (!string.IsNullOrWhiteSpace(invoice.CustomerName))
        {
            AddMetaRow(meta, fs, "العميل", invoice.CustomerName, "", "");
        }

        // ── Products table ────────────────────────────────────────────────────
        AddSectionHeading(doc, "بنود الفاتورة", fs);
        var products = a4
            ? MakeTable(doc, 4.2, 0.9, 1.6, 1.5)
            : MakeTable(doc, 4.0, 1.0, 1.5, 1.7);
        AddHeaderRow(products, fs, "الصنف", "الكمية", "سعر الوحدة", "الإجمالي");
        int rowIdx = 0;
        foreach (var line in invoice.Lines)
        {
            string name = line.ProductName;
            if (line.Discount > 0) name += $"  (خصم: {line.Discount:N2})";
            AddDataRow(products, fs, rowIdx++,
                name,
                line.Quantity.ToString("N2"),
                line.UnitPrice.ToString("N2"),
                line.Total.ToString("N2"));
        }

        // ── Totals ────────────────────────────────────────────────────────────
        AddDivider(doc, fs);
        var subtotal        = invoice.Lines.Sum(l => l.Total);
        var percentDiscount = decimal.Round(subtotal * invoice.DiscountPercent / 100m, 2, MidpointRounding.AwayFromZero);
        var fixedDiscount   = Math.Max(0, subtotal - percentDiscount - invoice.Total);

        var totals = MakeTable(doc, 3.0, 1.5);
        AddTotalsRow(totals, fs, "إجمالي المنتجات",   $"{subtotal:N2} ج.م");
        if (invoice.DiscountPercent > 0)
            AddTotalsRow(totals, fs, $"خصم النسبة ({invoice.DiscountPercent:N2}%)", $"- {percentDiscount:N2} ج.م");
        if (fixedDiscount > 0)
            AddTotalsRow(totals, fs, "الخصم الثابت", $"- {fixedDiscount:N2} ج.م");
        AddBoldTotalsRow(totals, fs + 1, "صافي الفاتورة", $"{invoice.Total:N2} ج.م");

        // ── Installment / payment section ─────────────────────────────────────
        bool isInstallment = invoice.Type.Contains("قسط", StringComparison.OrdinalIgnoreCase) || installments.Count > 0;
        if (isInstallment)
        {
            AddSectionHeading(doc, "بيانات التقسيط", fs);
            var schedule         = installments.OrderBy(x => x.Sequence).ToList();
            var originalFinanced = schedule.Count > 0
                ? schedule.Sum(x => x.Amount)
                : invoice.InitialPayment.HasValue ? invoice.Total - invoice.InitialPayment.Value : invoice.Remaining;

            var payInfo = MakeTable(doc, 3.0, 1.5);
            if (invoice.InitialPayment.HasValue)
                AddTotalsRow(payInfo, fs, "المقدم عند البيع", $"{invoice.InitialPayment.Value:N2} ج.م");
            AddTotalsRow(payInfo, fs, "المبلغ الممول",   $"{originalFinanced:N2} ج.م");
            AddTotalsRow(payInfo, fs, "المتبقي الحالي",  $"{invoice.Remaining:N2} ج.م");
            var duration = invoice.InstallmentMonths > 0 ? invoice.InstallmentMonths : schedule.Count;
            if (duration > 0)
                AddTotalsRow(payInfo, fs, "مدة التقسيط", $"{duration} شهر");

            if (schedule.Count > 0)
            {
                var firstOpen           = schedule.FirstOrDefault(x => x.Remaining > 0);
                var representativeAmt   = schedule[0].Amount;
                bool sameAmount         = schedule.All(x => x.Amount == representativeAmt);
                string installmentValue = sameAmount
                    ? $"{representativeAmt:N2} ج.م"
                    : $"من {schedule.Min(x => x.Amount):N2} إلى {schedule.Max(x => x.Amount):N2} ج.م";
                AddTotalsRow(payInfo, fs, "قيمة القسط", installmentValue);
                if (firstOpen is not null)
                    AddTotalsRow(payInfo, fs, "القسط القادم",
                        $"رقم {firstOpen.Sequence} — استحقاق {firstOpen.DueDate:yyyy/MM/dd}");

                // Installment schedule table
                AddSectionHeading(doc, "جدول الأقساط", fs);
                var instTable = MakeTable(doc, 0.7, 1.8, 1.4, 1.4, 1.4, 1.5);
                AddHeaderRow(instTable, fs, "#", "تاريخ الاستحقاق", "قيمة القسط", "المدفوع", "المتبقي", "الحالة");
                int iIdx = 0;
                foreach (var inst in schedule)
                    AddDataRow(instTable, fs, iIdx++,
                        inst.Sequence.ToString(),
                        inst.DueDate.ToString("yyyy/MM/dd"),
                        inst.Amount.ToString("N2"),
                        inst.PaidAmount.ToString("N2"),
                        inst.Remaining.ToString("N2"),
                        inst.Status);
            }
        }
        else
        {
            var payInfo = MakeTable(doc, 3.0, 1.5);
            AddTotalsRow(payInfo, fs, "المدفوع",  $"{invoice.Paid:N2} ج.م");
            if (invoice.Remaining > 0)
                AddTotalsRow(payInfo, fs, "المتبقي", $"{invoice.Remaining:N2} ج.م");
        }

        AddFooter(doc, settings, fs);
        return doc;
    }

    /// <summary>Quick-sale preview invoice built from cart items (not yet saved).</summary>
    internal static FlowDocument CreatePreviewInvoice(
        IReadOnlyList<InvoiceLine> cart, string saleType, Customer? customer,
        decimal discountPercent, decimal fixedDiscount, decimal paid,
        CompanySettings settings)
    {
        bool thermal58 = settings.PaperSize.Contains("58");
        bool a4        = settings.PaperSize.Equals("A4", StringComparison.OrdinalIgnoreCase);
        double width   = a4 ? 793.7 : thermal58 ? 219.2 : 302.4;
        double fs      = a4 ? BodySize : thermal58 ? 8.5 : 9.5;

        var doc = MakeDocument(width, a4 ? 1122.5 : 900, a4 ? 36 : 10, fs);

        if (settings.ShowStoreInfo)
            AddCompanyHeader(doc, settings, fs, a4);

        AddCentredPara(doc, "فاتورة بيع — معاينة", a4 ? 18 : fs + 3, bold: true);
        AddCentredPara(doc, $"التاريخ: {DateTime.Now:yyyy/MM/dd HH:mm}  |  نوع البيع: {saleType}", fs, muted: true);
        AddDivider(doc, fs);

        if (customer is not null)
        {
            var meta = MetaTable(doc);
            AddMetaRow(meta, fs, "كود العميل", customer.Code, "اسم العميل", customer.Name);
            if (!string.IsNullOrWhiteSpace(customer.Phone))
                AddMetaRow(meta, fs, "هاتف العميل", customer.Phone, "", "");
        }

        AddSectionHeading(doc, "بنود الفاتورة", fs);
        var products = MakeTable(doc, 4.2, 0.9, 1.6, 1.5);
        AddHeaderRow(products, fs, "الصنف", "الكمية", "سعر الوحدة", "الإجمالي");
        int rowIdx = 0;
        foreach (var line in cart)
        {
            string name = line.ProductName;
            if (line.Discount > 0) name += $"  (خصم: {line.Discount:N2})";
            AddDataRow(products, fs, rowIdx++,
                name,
                line.Quantity.ToString("N2"),
                line.UnitPrice.ToString("N2"),
                line.Total.ToString("N2"));
        }

        AddDivider(doc, fs);
        var subtotal        = cart.Sum(l => l.Total);
        var pctDiscount     = decimal.Round(subtotal * discountPercent / 100m, 2, MidpointRounding.AwayFromZero);
        var total           = Math.Max(0, subtotal - pctDiscount - fixedDiscount);
        var remaining       = Math.Max(0, total - paid);

        var totals = MakeTable(doc, 3.0, 1.5);
        AddTotalsRow(totals, fs, "إجمالي المنتجات", $"{subtotal:N2} ج.م");
        if (discountPercent > 0)
            AddTotalsRow(totals, fs, $"خصم النسبة ({discountPercent:N2}%)", $"- {pctDiscount:N2} ج.م");
        if (fixedDiscount > 0)
            AddTotalsRow(totals, fs, "الخصم الثابت", $"- {fixedDiscount:N2} ج.م");
        AddBoldTotalsRow(totals, fs + 1, "صافي الفاتورة", $"{total:N2} ج.م");
        AddTotalsRow(totals, fs, "المدفوع",  $"{paid:N2} ج.م");
        if (remaining > 0)
            AddTotalsRow(totals, fs, "المتبقي", $"{remaining:N2} ج.م");

        AddDivider(doc, fs);
        AddCentredPara(doc, "معاينة قبل الحفظ — البيانات غير محفوظة بعد", SmallSize, muted: true);
        AddFooter(doc, settings, fs);
        return doc;
    }

    /// <summary>Customer account statement.</summary>
    internal static FlowDocument CreateCustomerStatement(
        Customer customer, IReadOnlyList<StatementRow> rows,
        decimal invoiceTotal, decimal collectedTotal,
        CompanySettings settings)
    {
        var doc = MakeDocument(850, 1122.5, 36, BodySize);

        if (settings.ShowStoreInfo)
            AddCompanyHeader(doc, settings, BodySize, a4: true);

        AddCentredPara(doc, "كشف حساب العميل", TitleSize, bold: true);
        AddDivider(doc, BodySize);

        var meta = MetaTable(doc);
        AddMetaRow(meta, BodySize, "العميل",    customer.Name,  "كود العميل", customer.Code);
        AddMetaRow(meta, BodySize, "الهاتف",    customer.Phone, "الرصيد الحالي", $"{customer.Balance:N2} ج.م");
        AddMetaRow(meta, BodySize, "إجمالي الفواتير", $"{invoiceTotal:N2} ج.م",
                                   "إجمالي التحصيلات",  $"{collectedTotal:N2} ج.م");
        AddMetaRow(meta, BodySize, "تاريخ الكشف", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), "", "");

        AddSectionHeading(doc, "تفاصيل الحساب", BodySize);
        var table = MakeTable(doc, 1.1, 0.8, 1.3, 1.2, 1.0, 1.0, 1.0, 1.5);
        AddHeaderRow(table, BodySize, "التاريخ", "النوع", "الرقم", "البيان", "القيمة", "المدفوع", "الرصيد", "الحالة/ملاحظات");
        int idx = 0;
        foreach (var r in rows)
            AddDataRow(table, BodySize, idx++,
                r.Date.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture),
                r.Kind, r.Number, r.Description,
                r.Amount.ToString("N2"), r.Paid.ToString("N2"),
                r.Balance.ToString("N2"), r.Status);

        AddFooter(doc, settings, BodySize);
        return doc;
    }

    /// <summary>Supplier account statement.</summary>
    internal static FlowDocument CreateSupplierStatement(
        Supplier supplier,
        IEnumerable<Purchase> purchases,
        IEnumerable<SupplierPayment> payments,
        CompanySettings settings)
    {
        var doc = MakeDocument(850, 1122.5, 36, BodySize);

        if (settings.ShowStoreInfo)
            AddCompanyHeader(doc, settings, BodySize, a4: true);

        AddCentredPara(doc, $"كشف حساب المورد", TitleSize, bold: true);
        AddDivider(doc, BodySize);

        var meta = MetaTable(doc);
        AddMetaRow(meta, BodySize, "المورد", supplier.Name, "كود المورد", supplier.Code);
        AddMetaRow(meta, BodySize, "الهاتف", supplier.Phone, "الرصيد المستحق", $"{supplier.Balance:N2} ج.م");
        AddMetaRow(meta, BodySize, "تاريخ الكشف", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), "", "");

        AddSectionHeading(doc, "تفاصيل حساب المورد", BodySize);
        var table = MakeTable(doc, 1.1, 1.2, 1.3, 1.1, 1.1, 1.1, 1.5);
        AddHeaderRow(table, BodySize, "التاريخ", "النوع", "الرقم", "القيمة", "المدفوع", "الرصيد", "التفاصيل");

        var allRows = purchases
            .Select(p => (p.Date, "فاتورة شراء", p.Number, p.Total, p.Paid, p.Remaining, p.Status))
            .Concat(payments.Select(p => (p.Date, "دفعة مورد", p.ReceiptNumber, 0m, p.Amount, p.BalanceAfter, p.Method + " " + p.Note)))
            .OrderBy(r => r.Date)
            .ToList();

        int idx = 0;
        foreach (var (date, kind, num, val, paid, bal, detail) in allRows)
            AddDataRow(table, BodySize, idx++,
                date.ToString("yyyy/MM/dd"),
                kind, num,
                val.ToString("N2"), paid.ToString("N2"),
                bal.ToString("N2"), detail);

        AddFooter(doc, settings, BodySize);
        return doc;
    }

    /// <summary>Supplier installment schedule for one or all purchases.</summary>
    internal static FlowDocument CreateSupplierInstallmentSchedule(
        Supplier supplier,
        IEnumerable<Purchase> relatedPurchases,
        IEnumerable<SupplierInstallment> installments,
        CompanySettings settings)
    {
        var doc = MakeDocument(850, 1122.5, 36, BodySize);

        if (settings.ShowStoreInfo)
            AddCompanyHeader(doc, settings, BodySize, a4: true);

        AddCentredPara(doc, "جدول أقساط المورد", TitleSize, bold: true);
        AddDivider(doc, BodySize);

        var meta = MetaTable(doc);
        AddMetaRow(meta, BodySize, "المورد", supplier.Name, "كود المورد", supplier.Code);
        AddMetaRow(meta, BodySize, "تاريخ الطباعة", DateTime.Now.ToString("yyyy/MM/dd HH:mm"), "", "");

        var allInstallments = installments.OrderBy(i => i.PurchaseNumber).ThenBy(i => i.Sequence).ToList();

        foreach (var purchase in relatedPurchases.Where(p => allInstallments.Any(i => i.PurchaseNumber == p.Number)))
        {
            AddSectionHeading(doc, $"فاتورة شراء: {purchase.Number}", BodySize);
            var pMeta = MakeTable(doc, 2.0, 1.5, 2.0, 1.5);
            AddTotalsRow(pMeta, BodySize, "إجمالي الفاتورة", $"{purchase.Total:N2} ج.م", "الدفعة الأولى", $"{purchase.InitialPayment:N2} ج.م");
            AddTotalsRow(pMeta, BodySize, "المتبقي",         $"{purchase.Remaining:N2} ج.م", "عدد الأقساط",   $"{purchase.InstallmentMonths}");

            var table = MakeTable(doc, 0.6, 1.7, 1.3, 1.3, 1.3, 1.5, 1.5);
            AddHeaderRow(table, BodySize, "#", "تاريخ الاستحقاق", "قيمة القسط", "المدفوع", "المتبقي", "الحالة", "تاريخ السداد");
            int idx = 0;
            foreach (var inst in allInstallments.Where(i => i.PurchaseNumber == purchase.Number))
                AddDataRow(table, BodySize, idx++,
                    inst.Sequence.ToString(),
                    inst.DueDate.ToString("yyyy/MM/dd"),
                    inst.Amount.ToString("N2"),
                    inst.PaidAmount.ToString("N2"),
                    inst.Remaining.ToString("N2"),
                    inst.Status,
                    inst.PaidDate?.ToString("yyyy/MM/dd") ?? "—");
        }

        AddFooter(doc, settings, BodySize);
        return doc;
    }

    /// <summary>Installment schedule for a sales invoice.</summary>
    internal static FlowDocument CreateInstallmentScheduleDocument(
        string invoiceNumber, string customerName,
        IReadOnlyList<Installment> installments,
        CompanySettings settings)
    {
        bool a4   = settings.PaperSize.Equals("A4", StringComparison.OrdinalIgnoreCase);
        double fs = a4 ? BodySize : 9.5;
        double w  = a4 ? 793.7 : 302.4;

        var doc = MakeDocument(w, a4 ? 1122.5 : 900, a4 ? 36 : 10, fs);

        if (settings.ShowStoreInfo)
            AddCompanyHeader(doc, settings, fs, a4);

        AddCentredPara(doc, "جدول أقساط الفاتورة", a4 ? 18 : fs + 3, bold: true);
        AddDivider(doc, fs);

        var meta = MetaTable(doc);
        AddMetaRow(meta, fs, "رقم الفاتورة", invoiceNumber, "العميل", customerName);
        AddMetaRow(meta, fs, "عدد الأقساط", installments.Count.ToString(),
                              "إجمالي الأقساط", $"{installments.Sum(x => x.Amount):N2} ج.م");
        AddMetaRow(meta, fs, "المتبقي", $"{installments.Sum(x => x.Remaining):N2} ج.م",
                              "تاريخ الطباعة", DateTime.Now.ToString("yyyy/MM/dd"));

        AddSectionHeading(doc, "تفاصيل الأقساط", fs);
        var table = MakeTable(doc, 0.7, 1.8, 1.4, 1.4, 1.4, 1.5);
        AddHeaderRow(table, fs, "#", "تاريخ الاستحقاق", "قيمة القسط", "المدفوع", "المتبقي", "الحالة");
        int idx = 0;
        foreach (var inst in installments.OrderBy(x => x.Sequence))
            AddDataRow(table, fs, idx++,
                inst.Sequence.ToString(),
                inst.DueDate.ToString("yyyy/MM/dd"),
                inst.Amount.ToString("N2"),
                inst.PaidAmount.ToString("N2"),
                inst.Remaining.ToString("N2"),
                inst.Status);

        AddFooter(doc, settings, fs);
        return doc;
    }

    /// <summary>Generic record document for non-invoice modules (Products, Collections, etc.).</summary>
    internal static FlowDocument CreateRecordDocument(
        string moduleName, object record, CompanySettings settings)
    {
        bool a4   = settings.PaperSize.Equals("A4", StringComparison.OrdinalIgnoreCase);
        double fs = a4 ? BodySize : 9.5;
        double w  = a4 ? 793.7 : 302.4;

        var doc = MakeDocument(w, a4 ? 1122.5 : 900, a4 ? 36 : 10, fs);

        if (settings.ShowStoreInfo)
            AddCompanyHeader(doc, settings, fs, a4);

        AddCentredPara(doc, moduleName, a4 ? 18 : fs + 3, bold: true);
        AddCentredPara(doc, $"تاريخ الطباعة: {DateTime.Now:yyyy/MM/dd HH:mm}", SmallSize, muted: true);
        AddDivider(doc, fs);

        AddSectionHeading(doc, "تفاصيل السجل", fs);
        var table = MakeTable(doc, 2.0, 3.0);
        AddHeaderRow(table, fs, "الحقل", "القيمة");
        int idx = 0;
        foreach (var prop in record.GetType().GetProperties())
        {
            var val = prop.GetValue(record)?.ToString() ?? "—";
            AddDataRow(table, fs, idx++, prop.Name.Replace('_', ' '), val);
        }

        AddFooter(doc, settings, fs);
        return doc;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  PREVIEW WINDOW FACTORY
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates and returns a preview Window with a FlowDocumentScrollViewer
    /// and a Print button. Caller is responsible for calling ShowDialog().
    /// </summary>
    internal static Window CreatePreviewWindow(
        FlowDocument doc, string title, string printJobName,
        CompanySettings settings, Window? owner = null)
    {
        bool a4 = settings.PaperSize.Equals("A4", StringComparison.OrdinalIgnoreCase);

        var viewer = new FlowDocumentScrollViewer
        {
            Document = doc,
            IsToolBarVisible = false,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = Brushes.White,
            Padding = new Thickness(12)
        };

        var printButton = new Button
        {
            Content = "طباعة",
            Padding = new Thickness(22, 10, 22, 10),
            Margin = new Thickness(8),
            HorizontalAlignment = HorizontalAlignment.Left,
            Background = HeaderBg,
            Foreground = Brushes.White,
            FontSize = BodySize,
            FontWeight = FontWeights.SemiBold
        };

        var closeButton = new Button
        {
            Content = "إغلاق",
            Padding = new Thickness(16, 10, 16, 10),
            Margin = new Thickness(0, 8, 8, 8),
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var buttonBar = new DockPanel { Background = new SolidColorBrush(Color.FromRgb(248, 249, 252)) };
        DockPanel.SetDock(printButton, Dock.Left);
        DockPanel.SetDock(closeButton, Dock.Right);
        buttonBar.Children.Add(printButton);
        buttonBar.Children.Add(closeButton);

        var layout = new DockPanel();
        DockPanel.SetDock(buttonBar, Dock.Bottom);
        layout.Children.Add(buttonBar);
        layout.Children.Add(viewer);

        var window = new Window
        {
            Title = title,
            Width  = a4 ? 870 : 500,
            Height = 780,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = owner,
            FlowDirection = FlowDirection.RightToLeft,
            Background = Brushes.White,
            Content = layout
        };

        printButton.Click += (_, _) => ExecutePrint(doc, printJobName, settings);
        closeButton.Click += (_, _) => window.Close();

        return window;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  PRIVATE HELPERS — document structure
    // ═════════════════════════════════════════════════════════════════════════

    private static FlowDocument MakeDocument(double pageWidth, double pageHeight, double padding, double fontSize) =>
        new()
        {
            FlowDirection = FlowDirection.RightToLeft,
            FontFamily    = new FontFamily("Arial"),
            FontSize      = fontSize,
            PageWidth     = pageWidth,
            PageHeight    = pageHeight,
            PagePadding   = new Thickness(padding),
            ColumnWidth   = double.PositiveInfinity
        };

    private static void AddCompanyHeader(FlowDocument doc, CompanySettings settings, double fs, bool a4)
    {
        // Company name
        if (!string.IsNullOrWhiteSpace(settings.StoreName))
        {
            var name = new Paragraph(new Run(settings.StoreName))
            {
                FontSize      = a4 ? TitleSize : fs + 4,
                FontWeight    = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Foreground    = HeaderBg,
                Margin        = new Thickness(0, 0, 0, 4)
            };
            doc.Blocks.Add(name);
        }

        // Contact details row
        var details = new List<string>();
        if (!string.IsNullOrWhiteSpace(settings.Address))   details.Add("📍 " + settings.Address);
        if (!string.IsNullOrWhiteSpace(settings.Phone))     details.Add("📞 " + settings.Phone);
        if (!string.IsNullOrWhiteSpace(settings.TaxNumber)) details.Add("رقم ضريبي: " + settings.TaxNumber);

        if (details.Count > 0)
        {
            var contactPara = new Paragraph(new Run(string.Join("   |   ", details)))
            {
                FontSize      = a4 ? SmallSize + 0.5 : fs - 1,
                TextAlignment = TextAlignment.Center,
                Foreground    = MutedFg,
                Margin        = new Thickness(0, 0, 0, 6)
            };
            doc.Blocks.Add(contactPara);
        }
    }

    private static void AddDivider(FlowDocument doc, double fs)
    {
        // A thin horizontal rule rendered as a paragraph with bottom border
        var p = new Paragraph
        {
            BorderBrush     = DividerBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Margin          = new Thickness(0, 4, 0, 8),
            FontSize        = 1
        };
        doc.Blocks.Add(p);
    }

    private static void AddCentredPara(FlowDocument doc, string text, double size, bool bold = false, bool muted = false)
    {
        var p = new Paragraph(new Run(text))
        {
            TextAlignment = TextAlignment.Center,
            FontSize      = size,
            FontWeight    = bold ? FontWeights.Bold : FontWeights.Normal,
            Foreground    = muted ? MutedFg : Brushes.Black,
            Margin        = new Thickness(0, 2, 0, 2)
        };
        doc.Blocks.Add(p);
    }

    private static void AddSectionHeading(FlowDocument doc, string text, double fs)
    {
        var p = new Paragraph(new Run(text))
        {
            FontSize      = fs + 1,
            FontWeight    = FontWeights.Bold,
            Foreground    = SubheadFg,
            Margin        = new Thickness(0, 10, 0, 4),
            BorderBrush   = SubheadFg,
            BorderThickness = new Thickness(0, 0, 3, 0),
            Padding       = new Thickness(0, 0, 6, 0)
        };
        doc.Blocks.Add(p);
    }

    // ── 2-column meta table (label | value | label | value) ──────────────────
    private static Table MetaTable(FlowDocument doc)
    {
        var t = new Table
        {
            CellSpacing = 0,
            Margin      = new Thickness(0, 4, 0, 8)
        };
        t.Columns.Add(new TableColumn { Width = new GridLength(1.2, GridUnitType.Star) });
        t.Columns.Add(new TableColumn { Width = new GridLength(1.8, GridUnitType.Star) });
        t.Columns.Add(new TableColumn { Width = new GridLength(1.2, GridUnitType.Star) });
        t.Columns.Add(new TableColumn { Width = new GridLength(1.8, GridUnitType.Star) });
        doc.Blocks.Add(t);
        return t;
    }

    private static void AddMetaRow(Table table, double fs, string label1, string value1, string label2, string value2)
    {
        var group = EnsureGroup(table);
        var row = new TableRow();
        row.Cells.Add(MetaLabelCell(label1, fs));
        row.Cells.Add(MetaValueCell(value1, fs));
        row.Cells.Add(MetaLabelCell(label2, fs));
        row.Cells.Add(MetaValueCell(value2, fs));
        group.Rows.Add(row);
    }

    private static TableCell MetaLabelCell(string text, double fs) =>
        new(new Paragraph(new Run(text))
        {
            FontWeight = FontWeights.SemiBold,
            FontSize   = fs,
            Foreground = SubheadFg,
            Margin     = new Thickness(2)
        })
        {
            BorderBrush     = BorderBrush,
            BorderThickness = new Thickness(0.4),
            Padding         = new Thickness(4, 3, 4, 3),
            Background      = AltRowBg
        };

    private static TableCell MetaValueCell(string text, double fs) =>
        new(new Paragraph(new Run(text))
        {
            FontSize = fs,
            Margin   = new Thickness(2)
        })
        {
            BorderBrush     = BorderBrush,
            BorderThickness = new Thickness(0.4),
            Padding         = new Thickness(4, 3, 4, 3)
        };

    // ── Data table ────────────────────────────────────────────────────────────

    private static Table MakeTable(FlowDocument doc, params double[] weights)
    {
        var table = new Table
        {
            CellSpacing      = 0,
            BorderBrush      = BorderBrush,
            BorderThickness  = new Thickness(0.7),
            Margin           = new Thickness(0, 3, 0, 6)
        };
        foreach (var w in weights)
            table.Columns.Add(new TableColumn { Width = new GridLength(w, GridUnitType.Star) });
        doc.Blocks.Add(table);
        return table;
    }

    private static void AddHeaderRow(Table table, double fs, params string[] headers)
    {
        var group = EnsureGroup(table);
        var row = new TableRow { Background = HeaderBg };
        foreach (var h in headers)
            row.Cells.Add(new TableCell(
                new Paragraph(new Run(h))
                {
                    Margin        = new Thickness(1),
                    FontSize      = fs,
                    FontWeight    = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center,
                    Foreground    = HeaderFg
                })
            {
                BorderBrush     = new SolidColorBrush(Color.FromRgb(15, 40, 75)),
                BorderThickness = new Thickness(0.5),
                Padding         = new Thickness(4, 5, 4, 5)
            });
        group.Rows.Add(row);
    }

    private static void AddDataRow(Table table, double fs, int rowIndex, params string[] cells)
    {
        var group = EnsureGroup(table);
        var row   = new TableRow { Background = rowIndex % 2 == 1 ? AltRowBg : Brushes.White };
        foreach (var cell in cells)
            row.Cells.Add(new TableCell(
                new Paragraph(new Run(cell))
                {
                    Margin        = new Thickness(1),
                    FontSize      = fs,
                    TextAlignment = TextAlignment.Center
                })
            {
                BorderBrush     = BorderBrush,
                BorderThickness = new Thickness(0.4),
                Padding         = new Thickness(3, 4, 3, 4)
            });
        group.Rows.Add(row);
    }

    private static void AddTotalsRow(Table table, double fs, params string[] cells)
    {
        var group = EnsureGroup(table);
        var row   = new TableRow { Background = TotalsBg };
        foreach (var cell in cells)
            row.Cells.Add(new TableCell(
                new Paragraph(new Run(cell))
                {
                    Margin        = new Thickness(1),
                    FontSize      = fs,
                    TextAlignment = TextAlignment.Center
                })
            {
                BorderBrush     = BorderBrush,
                BorderThickness = new Thickness(0.4),
                Padding         = new Thickness(4, 4, 4, 4)
            });
        group.Rows.Add(row);
    }

    private static void AddBoldTotalsRow(Table table, double fs, params string[] cells)
    {
        var group = EnsureGroup(table);
        var row   = new TableRow { Background = HeaderBg };
        foreach (var cell in cells)
            row.Cells.Add(new TableCell(
                new Paragraph(new Run(cell))
                {
                    Margin        = new Thickness(1),
                    FontSize      = fs,
                    FontWeight    = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center,
                    Foreground    = HeaderFg
                })
            {
                BorderBrush     = new SolidColorBrush(Color.FromRgb(15, 40, 75)),
                BorderThickness = new Thickness(0.5),
                Padding         = new Thickness(4, 5, 4, 5)
            });
        group.Rows.Add(row);
    }

    private static void AddFooter(FlowDocument doc, CompanySettings settings, double fs)
    {
        AddDivider(doc, fs);
        if (settings.ShowStoreInfo)
        {
            if (!string.IsNullOrWhiteSpace(settings.Address))
                AddCentredPara(doc, "📍 " + settings.Address, SmallSize, muted: true);
            if (!string.IsNullOrWhiteSpace(settings.Phone))
                AddCentredPara(doc, "للتواصل: " + settings.Phone, SmallSize, muted: true);
        }
        AddCentredPara(doc, "شكراً لتعاملكم معنا  ✦  Smart ERP", SmallSize + 1, bold: true);
        AddCentredPara(doc, $"تاريخ الطباعة: {DateTime.Now:yyyy/MM/dd HH:mm}", SmallSize - 0.5, muted: true);
    }

    private static TableRowGroup EnsureGroup(Table table)
    {
        if (table.RowGroups.Count == 0)
        {
            var g = new TableRowGroup();
            table.RowGroups.Add(g);
        }
        return table.RowGroups[0];
    }

    // ── Print execution ───────────────────────────────────────────────────────

    private static void ExecutePrint(FlowDocument doc, string jobName, CompanySettings settings)
    {
        try
        {
            var dialog = new PrintDialog();
            try
            {
                if (!string.IsNullOrWhiteSpace(settings.PrinterName))
                {
                    var queue = new System.Printing.LocalPrintServer()
                        .GetPrintQueues()
                        .FirstOrDefault(q => q.FullName.Equals(settings.PrinterName, StringComparison.OrdinalIgnoreCase));
                    if (queue is not null) dialog.PrintQueue = queue;
                }
                var ticket     = dialog.PrintTicket ?? new System.Printing.PrintTicket();
                double pw      = settings.PaperSize.Contains("58") ? 219.2 : settings.PaperSize.Equals("A4", StringComparison.OrdinalIgnoreCase) ? 793.7 : 302.4;
                double ph      = settings.PaperSize.Equals("A4", StringComparison.OrdinalIgnoreCase) ? 1122.5 : 720;
                ticket.PageMediaSize  = new System.Printing.PageMediaSize(pw, ph);
                ticket.PageOrientation = System.Printing.PageOrientation.Portrait;
                dialog.PrintTicket = ticket;
            }
            catch { /* Keep printer defaults if driver rejects the paper setting. */ }

            if (dialog.ShowDialog() == true)
                dialog.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, jobName);
        }
        catch (Exception ex)
        {
            MessageBox.Show("تعذرت الطباعة:\n" + ex.Message, "خطأ في الطباعة", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
