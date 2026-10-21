using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using SmartErpDesktop.Models;

namespace SmartErpDesktop.Views;

internal static class InvoiceReceiptDocument
{
    internal static FlowDocument Create(Invoice invoice, Customer? customer, CompanySettings settings, IReadOnlyList<Installment> installments)
    {
        var thermal58 = settings.PaperSize.Contains("58");
        var a4 = settings.PaperSize.Equals("A4", StringComparison.OrdinalIgnoreCase);
        var width = a4 ? 793.7 : thermal58 ? 219.2 : 302.4;
        var fontSize = a4 ? 11 : thermal58 ? 8.5 : 9.5;
        var document = new FlowDocument
        {
            FlowDirection = FlowDirection.RightToLeft,
            FontFamily = new FontFamily("Arial"),
            FontSize = fontSize,
            PageWidth = width,
            PageHeight = a4 ? 1122.5 : 720,
            PagePadding = new Thickness(a4 ? 28 : 8),
            ColumnWidth = double.PositiveInfinity
        };

        Paragraph Para(string text, double size = 0, bool bold = false, TextAlignment align = TextAlignment.Right)
        {
            var p = new Paragraph(new Run(text))
            {
                Margin = new Thickness(0, 2, 0, 2),
                FontSize = size > 0 ? size : fontSize,
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                TextAlignment = align
            };
            document.Blocks.Add(p);
            return p;
        }

        void AddRule()
        {
            var p = Para("ــــــــــــــــــــــــــــــــــــــــــــــــــــــــــــــ", fontSize - 1, false, TextAlignment.Center);
            p.Margin = new Thickness(0, 1, 0, 1);
        }

        Table MakeTable(params double[] weights)
        {
            var table = new Table { CellSpacing = 0, BorderBrush = Brushes.Black, BorderThickness = new Thickness(0.7), Margin = new Thickness(0, 3, 0, 3) };
            foreach (var weight in weights) table.Columns.Add(new TableColumn { Width = new GridLength(weight, GridUnitType.Star) });
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
                    Margin = new Thickness(1),
                    FontSize = fontSize,
                    FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                    TextAlignment = TextAlignment.Center
                };
                row.Cells.Add(new TableCell(paragraph)
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(0.35),
                    Padding = new Thickness(2)
                });
            }
            group.Rows.Add(row);
        }

        if (settings.ShowStoreInfo)
        {
            if (!string.IsNullOrWhiteSpace(settings.StoreName)) Para(settings.StoreName, a4 ? 18 : fontSize + 3, true, TextAlignment.Center);
            if (!string.IsNullOrWhiteSpace(settings.Address)) Para(settings.Address, fontSize - 0.5, false, TextAlignment.Center);
            if (!string.IsNullOrWhiteSpace(settings.Phone)) Para("هاتف: " + settings.Phone, fontSize - 0.5, false, TextAlignment.Center);
            if (!string.IsNullOrWhiteSpace(settings.TaxNumber)) Para("الرقم الضريبي: " + settings.TaxNumber, fontSize - 0.5, false, TextAlignment.Center);
        }

        Para("إيصال بيع", a4 ? 16 : fontSize + 2, true, TextAlignment.Center);
        AddRule();
        Para($"رقم الفاتورة: {invoice.Number}     التاريخ: {invoice.Date:yyyy/MM/dd}", bold: true);
        Para($"نوع العملية: {invoice.Type}     الحالة: {invoice.Status}");
        if (customer is not null)
        {
            Para($"كود العميل: {customer.Code}     العميل: {customer.Name}");
            if (!string.IsNullOrWhiteSpace(customer.Address)) Para("المنطقة / العنوان: " + customer.Address);
            if (!string.IsNullOrWhiteSpace(customer.Phone)) Para("الموبايل: " + customer.Phone);
        }
        else if (!string.IsNullOrWhiteSpace(invoice.CustomerName)) Para("العميل: " + invoice.CustomerName);

        var products = MakeTable(4.1, 0.9, 1.5, 1.7);
        AddRow(products, ("الصنف", true), ("الكمية", true), ("سعر الوحدة", true), ("الإجمالي", true));
        foreach (var line in invoice.Lines)
        {
            var productName = line.ProductName;
            if (line.Discount > 0) productName += $"\nخصم السطر: {line.Discount:N2} ج.م";
            AddRow(products,
                (productName, false),
                (line.Quantity.ToString("N2"), false),
                (line.UnitPrice.ToString("N2"), false),
                (line.Total.ToString("N2"), false));
        }
        document.Blocks.Add(products);

        var subtotal = invoice.Lines.Sum(line => line.Total);
        var percentDiscount = decimal.Round(subtotal * invoice.DiscountPercent / 100m, 2, MidpointRounding.AwayFromZero);
        var fixedDiscount = Math.Max(0, subtotal - percentDiscount - invoice.Total);
        Para($"إجمالي المنتجات: {subtotal:N2} ج.م");
        if (invoice.DiscountPercent > 0) Para($"خصم النسبة ({invoice.DiscountPercent:N2}%): {percentDiscount:N2} ج.م");
        if (fixedDiscount > 0) Para($"الخصم الثابت: {fixedDiscount:N2} ج.م");
        Para($"صافي الفاتورة: {invoice.Total:N2} ج.م", fontSize + 1, true);

        var isInstallment = invoice.Type.Contains("قسط", StringComparison.OrdinalIgnoreCase) || installments.Count > 0;
        if (isInstallment)
        {
            var schedule = installments.OrderBy(x => x.Sequence).ToList();
            var originalFinanced = schedule.Count > 0 ? schedule.Sum(x => x.Amount) : invoice.InitialPayment.HasValue ? invoice.Total - invoice.InitialPayment.Value : invoice.Remaining;
            if (invoice.InitialPayment.HasValue) Para($"المقدم عند البيع: {invoice.InitialPayment.Value:N2} ج.م");
            Para($"المبلغ الممول: {originalFinanced:N2} ج.م");
            Para($"المتبقي الحالي: {invoice.Remaining:N2} ج.م");
            var duration = invoice.InstallmentMonths > 0 ? invoice.InstallmentMonths : schedule.Count;
            if (duration > 0) Para($"مدة التقسيط: {duration} شهر");
            if (schedule.Count > 0)
            {
                var firstOpen = schedule.FirstOrDefault(x => x.Remaining > 0);
                var representativeAmount = schedule[0].Amount;
                var sameAmount = schedule.All(x => x.Amount == representativeAmount);
                Para($"قيمة القسط: {(sameAmount ? representativeAmount.ToString("N2") : $"من {schedule.Min(x => x.Amount):N2} إلى {schedule.Max(x => x.Amount):N2}")} ج.م");
                if (firstOpen is not null) Para($"القسط القادم: رقم {firstOpen.Sequence} - استحقاق {firstOpen.DueDate:yyyy/MM/dd}");
            }
        }
        else
        {
            Para($"المدفوع: {invoice.Paid:N2} ج.م");
            if (invoice.Remaining > 0) Para($"المتبقي: {invoice.Remaining:N2} ج.م");
        }

        AddRule();
        if (settings.ShowStoreInfo && !string.IsNullOrWhiteSpace(settings.Address)) Para(settings.Address, fontSize - 0.5, false, TextAlignment.Center);
        if (settings.ShowStoreInfo && !string.IsNullOrWhiteSpace(settings.Phone)) Para("للتواصل: " + settings.Phone, fontSize - 0.5, false, TextAlignment.Center);
        Para("شكراً لثقتكم بنا", fontSize, true, TextAlignment.Center);
        return document;
    }
}
