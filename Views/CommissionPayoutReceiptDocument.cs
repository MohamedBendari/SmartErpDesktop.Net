using System.Windows;
using System.Windows.Documents;
using SmartErpDesktop.Models;
using SmartErpDesktop.Services;

namespace SmartErpDesktop.Views;

internal static class CommissionPayoutReceiptDocument
{
    internal static FlowDocument Create(RepresentativePayout payout, MockErpService service)
    {
        var doc = new FlowDocument
        {
            FlowDirection = FlowDirection.RightToLeft,
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
            FontSize = 14,
            PagePadding = new Thickness(28),
            ColumnWidth = 720
        };
        var title = new Paragraph(new Run("إيصال صرف عمولة مندوب"))
        {
            FontSize = 21,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 18)
        };
        doc.Blocks.Add(title);
        AddLine(doc, "رقم الإيصال", payout.ReceiptNumber);
        AddLine(doc, "التاريخ", payout.Date.ToString("yyyy/MM/dd HH:mm"));
        AddLine(doc, "المندوب", $"{payout.RepresentativeName} ({payout.RepresentativeCode})");
        AddLine(doc, "المبلغ المصروف", payout.Amount.ToString("N2") + " ج.م");
        AddLine(doc, "طريقة الدفع", payout.Method);
        if (!string.IsNullOrWhiteSpace(payout.Notes)) AddLine(doc, "ملاحظات", payout.Notes);

        doc.Blocks.Add(new Paragraph(new Run("تفاصيل التسوية")) { FontWeight = FontWeights.Bold, Margin = new Thickness(0, 16, 0, 6) });
        var table = new Table { CellSpacing = 0 };
        table.Columns.Add(new TableColumn { Width = new GridLength(2, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(2, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        var group = new TableRowGroup();
        var header = new TableRow { Background = System.Windows.Media.Brushes.LightGray };
        AddCell(header, "الفاتورة"); AddCell(header, "إيصال التحصيل"); AddCell(header, "المبلغ");
        group.Rows.Add(header);
        foreach (var allocation in payout.Allocations)
        {
            var accrual = service.CommissionAccruals.FirstOrDefault(x => x.Id == allocation.AccrualId);
            var row = new TableRow();
            AddCell(row, accrual?.InvoiceNumber ?? "—");
            AddCell(row, accrual?.PaymentReceiptNumber ?? "—");
            AddCell(row, allocation.Amount.ToString("N2"));
            group.Rows.Add(row);
        }
        table.RowGroups.Add(group);
        doc.Blocks.Add(table);
        doc.Blocks.Add(new Paragraph(new Run("هذا إيصال صرف للمندوب، منفصل عن إيصالات تحصيل العملاء."))
        {
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 20, 0, 0)
        });
        return doc;
    }

    private static void AddLine(FlowDocument doc, string label, string value) =>
        doc.Blocks.Add(new Paragraph(new Run($"{label}: {value}")) { Margin = new Thickness(0, 2, 0, 2) });

    private static void AddCell(TableRow row, string text) =>
        row.Cells.Add(new TableCell(new Paragraph(new Run(text)) { Margin = new Thickness(4) }) { BorderBrush = System.Windows.Media.Brushes.Gray, BorderThickness = new Thickness(0.5) });
}
