using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using SmartErpDesktop.Models;

namespace SmartErpDesktop.Views;

/// <summary>
/// Entry-point for invoice FlowDocument creation.
/// A4 documents are delegated to ErpPrintDocument (professional enterprise layout).
/// Thermal (58 mm / 80 mm) documents use a compact receipt layout.
/// </summary>
internal static class InvoiceReceiptDocument
{
    internal static FlowDocument Create(
        Invoice invoice, Customer? customer,
        CompanySettings settings, IReadOnlyList<Installment> installments)
    {
        var representativeName = GetRepresentativeDisplayName(invoice);
        if (string.IsNullOrWhiteSpace(representativeName) || !ShouldShowRepresentative(invoice.Type, settings))
            representativeName = null;

        // A4 → full enterprise layout via the shared engine
        if (settings.PaperSize.Equals("A4", StringComparison.OrdinalIgnoreCase))
            return ErpPrintDocument.CreateInvoiceDocument(invoice, customer, settings, installments, representativeName);

        // Thermal receipt layout (compact, unchanged business logic)
        return CreateThermalReceipt(invoice, customer, settings, installments, representativeName);
    }

    internal static bool ShouldShowRepresentative(string invoiceType, CompanySettings settings)
    {
        var internalInvoice = invoiceType.Contains("داخلي", StringComparison.OrdinalIgnoreCase) ||
                              invoiceType.Contains("internal", StringComparison.OrdinalIgnoreCase);
        return internalInvoice
            ? settings.ShowRepresentativeOnInternalInvoice
            : settings.ShowRepresentativeOnCustomerInvoice;
    }

    internal static string GetRepresentativeDisplayName(Invoice invoice) =>
        string.IsNullOrWhiteSpace(invoice.RepresentativeName) ? invoice.RepresentativeCode : invoice.RepresentativeName;

    // ─────────────────────────────────────────────────────────────────────────
    // Compact thermal receipt (58 mm / 80 mm)
    // ─────────────────────────────────────────────────────────────────────────
    private static FlowDocument CreateThermalReceipt(
        Invoice invoice, Customer? customer,
        CompanySettings settings, IReadOnlyList<Installment> installments,
        string? representativeName)
    {
        bool thermal58 = settings.PaperSize.Contains("58");
        double width   = thermal58 ? 219.2 : 302.4;
        double fontSize = thermal58 ? 8.5 : 9.5;

        var document = new FlowDocument
        {
            FlowDirection = FlowDirection.RightToLeft,
            FontFamily    = new FontFamily("Arial"),
            FontSize      = fontSize,
            PageWidth     = width,
            PageHeight    = 900,
            PagePadding   = new Thickness(8),
            ColumnWidth   = double.PositiveInfinity
        };

        Paragraph Para(string text, double size = 0, bool bold = false,
                        TextAlignment align = TextAlignment.Right)
        {
            var p = new Paragraph(new Run(text))
            {
                Margin        = new Thickness(0, 2, 0, 2),
                FontSize      = size > 0 ? size : fontSize,
                FontWeight    = bold ? FontWeights.Bold : FontWeights.Normal,
                TextAlignment = align
            };
            document.Blocks.Add(p);
            return p;
        }

        void AddRule()
        {
            var p = Para("ـــــــــــــــــــــــــــــــــــــ", fontSize - 1, false, TextAlignment.Center);
            p.Margin = new Thickness(0, 1, 0, 1);
        }

        Table MakeTable(params double[] weights)
        {
            var table = new Table
            {
                CellSpacing     = 0,
                BorderBrush     = Brushes.Black,
                BorderThickness = new Thickness(0.7),
                Margin          = new Thickness(0, 3, 0, 3)
            };
            foreach (var w in weights)
                table.Columns.Add(new TableColumn { Width = new GridLength(w, GridUnitType.Star) });
            return table;
        }

        void AddRow(Table table, params (string Text, bool Bold)[] cells)
        {
            var group = table.RowGroups.FirstOrDefault();
            if (group is null) { group = new TableRowGroup(); table.RowGroups.Add(group); }
            var row = new TableRow();
            foreach (var (text, bold) in cells)
            {
                var paragraph = new Paragraph(new Run(text))
                {
                    Margin        = new Thickness(1),
                    FontSize      = fontSize,
                    FontWeight    = bold ? FontWeights.Bold : FontWeights.Normal,
                    TextAlignment = TextAlignment.Center
                };
                row.Cells.Add(new TableCell(paragraph)
                {
                    BorderBrush     = Brushes.Black,
                    BorderThickness = new Thickness(0.35),
                    Padding         = new Thickness(2)
                });
            }
            group.Rows.Add(row);
        }

        // Company header
        if (settings.ShowStoreInfo)
        {
            if (!string.IsNullOrWhiteSpace(settings.StoreName))
                Para(settings.StoreName, fontSize + 3, true, TextAlignment.Center);
            if (!string.IsNullOrWhiteSpace(settings.Address))
                Para(settings.Address, fontSize - 0.5, false, TextAlignment.Center);
            if (!string.IsNullOrWhiteSpace(settings.Phone))
                Para("هاتف: " + settings.Phone, fontSize - 0.5, false, TextAlignment.Center);
            if (!string.IsNullOrWhiteSpace(settings.TaxNumber))
                Para("الرقم الضريبي: " + settings.TaxNumber, fontSize - 0.5, false, TextAlignment.Center);
        }

        Para("إيصال بيع", fontSize + 2, true, TextAlignment.Center);
        AddRule();
        Para($"رقم الفاتورة: {invoice.Number}     التاريخ: {invoice.Date:yyyy/MM/dd}", bold: true);
        Para($"نوع العملية: {invoice.Type}     الحالة: {invoice.Status}");
        if (!string.IsNullOrWhiteSpace(representativeName))
            Para("المندوب: " + representativeName);

        if (customer is not null)
        {
            Para($"كود العميل: {customer.Code}     العميل: {customer.Name}");
            if (!string.IsNullOrWhiteSpace(customer.Address)) Para("المنطقة / العنوان: " + customer.Address);
            if (!string.IsNullOrWhiteSpace(customer.Phone))   Para("الموبايل: " + customer.Phone);
        }
        else if (!string.IsNullOrWhiteSpace(invoice.CustomerName))
        {
            Para("العميل: " + invoice.CustomerName);
        }

        var products = MakeTable(4.1, 0.9, 1.5, 1.7);
        AddRow(products, ("الصنف", true), ("الكمية", true), ("سعر الوحدة", true), ("الإجمالي", true));
        foreach (var line in invoice.Lines)
        {
            var productName = line.ProductName;
            if (line.Discount > 0) productName += $"\nخصم: {line.Discount:N2}";
            AddRow(products,
                (productName, false),
                (line.Quantity.ToString("N2"), false),
                (line.UnitPrice.ToString("N2"), false),
                (line.Total.ToString("N2"), false));
        }
        document.Blocks.Add(products);

        var subtotal        = invoice.Lines.Sum(l => l.Total);
        var percentDiscount = decimal.Round(subtotal * invoice.DiscountPercent / 100m, 2, MidpointRounding.AwayFromZero);
        var fixedDiscount   = Math.Max(0, subtotal - percentDiscount - invoice.Total);

        Para($"إجمالي المنتجات: {subtotal:N2} ج.م");
        if (invoice.DiscountPercent > 0)
            Para($"خصم النسبة ({invoice.DiscountPercent:N2}%): {percentDiscount:N2} ج.م");
        if (fixedDiscount > 0)
            Para($"الخصم الثابت: {fixedDiscount:N2} ج.م");
        Para($"صافي الفاتورة: {invoice.Total:N2} ج.م", fontSize + 1, true);

        bool isInstallment = invoice.Type.Contains("قسط", StringComparison.OrdinalIgnoreCase)
                          || installments.Count > 0;
        if (isInstallment)
        {
            var schedule = installments.OrderBy(x => x.Sequence).ToList();
            var originalFinanced = schedule.Count > 0
                ? schedule.Sum(x => x.Amount)
                : invoice.InitialPayment.HasValue
                    ? invoice.Total - invoice.InitialPayment.Value
                    : invoice.Remaining;

            if (invoice.InitialPayment.HasValue)
                Para($"المقدم عند البيع: {invoice.InitialPayment.Value:N2} ج.م");
            Para($"المبلغ الممول: {originalFinanced:N2} ج.م");
            Para($"المتبقي الحالي: {invoice.Remaining:N2} ج.م");
            var duration = invoice.InstallmentMonths > 0 ? invoice.InstallmentMonths : schedule.Count;
            if (duration > 0) Para($"مدة التقسيط: {duration} شهر");

            if (schedule.Count > 0)
            {
                var firstOpen           = schedule.FirstOrDefault(x => x.Remaining > 0);
                var representativeAmt   = schedule[0].Amount;
                bool sameAmount         = schedule.All(x => x.Amount == representativeAmt);
                string installmentValue = sameAmount
                    ? representativeAmt.ToString("N2")
                    : $"من {schedule.Min(x => x.Amount):N2} إلى {schedule.Max(x => x.Amount):N2}";
                Para($"قيمة القسط: {installmentValue} ج.م");
                if (firstOpen is not null)
                    Para($"القسط القادم: رقم {firstOpen.Sequence} - استحقاق {firstOpen.DueDate:yyyy/MM/dd}");
            }
        }
        else
        {
            Para($"المدفوع: {invoice.Paid:N2} ج.م");
            if (invoice.Remaining > 0) Para($"المتبقي: {invoice.Remaining:N2} ج.م");
        }

        AddRule();
        if (settings.ShowStoreInfo)
        {
            if (!string.IsNullOrWhiteSpace(settings.Address))
                Para(settings.Address, fontSize - 0.5, false, TextAlignment.Center);
            if (!string.IsNullOrWhiteSpace(settings.Phone))
                Para("للتواصل: " + settings.Phone, fontSize - 0.5, false, TextAlignment.Center);
        }
        Para("شكراً لثقتكم بنا", fontSize, true, TextAlignment.Center);
        return document;
    }
}
