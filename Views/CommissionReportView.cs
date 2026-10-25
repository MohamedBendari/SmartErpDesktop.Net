using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using Microsoft.Win32;
using SmartErpDesktop.Models;
using SmartErpDesktop.Services;

namespace SmartErpDesktop.Views;

public sealed class CommissionReportView : UserControl
{
    private sealed record ReportRow(string RepresentativeCode, string Representative, string AccrualDate, string SourceReference,
        string Invoice, string Product, string RuleSnapshot, decimal RatePercent, decimal EligibleBasis, decimal EligibleCollected, decimal Commission,
        string ApprovalStatus, string Approver, string ApprovalDate, decimal Paid, string PayoutReferences, decimal Outstanding,
        string AccrualId, CommissionAccrual Accrual);

    private readonly MockErpService service;
    private readonly ComboBox rep = new() { Width = 175, Margin = new Thickness(3) };
    private readonly DatePicker from = new() { Width = 135, Margin = new Thickness(3) };
    private readonly DatePicker to = new() { Width = 135, Margin = new Thickness(3) };
    private readonly TextBox invoice = new() { Width = 130, Margin = new Thickness(3), ToolTip = "Invoice number" };
    private readonly TextBox source = new() { Width = 140, Margin = new Thickness(3), ToolTip = "Collection receipt or source allocation" };
    private readonly TextBox payout = new() { Width = 130, Margin = new Thickness(3), ToolTip = "Payout receipt number" };
    private readonly ComboBox status = new() { Width = 130, Margin = new Thickness(3) };
    private readonly DataGrid grid = new() { IsReadOnly = true, CanUserAddRows = false, AutoGenerateColumns = true, Margin = new Thickness(3) };
    private readonly TextBlock totals = new() { Margin = new Thickness(6), FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap };
    private List<ReportRow> rows = new();

    public CommissionReportView(MockErpService service)
    {
        this.service = service;
        var root = new DockPanel { Margin = new Thickness(8) };
        var top = new StackPanel();
        var filters = new WrapPanel();
        rep.Items.Add("All representatives");
        foreach (var r in service.Representatives.OrderBy(x => x.Code)) rep.Items.Add(r.Code + " — " + r.Name);
        var knownCodes = service.Representatives.Select(x => x.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var saved in service.CommissionAccruals.Where(x => x is not null && !string.IsNullOrWhiteSpace(x.RepresentativeCode))
                     .GroupBy(x => x.RepresentativeCode, StringComparer.OrdinalIgnoreCase).Where(g => !knownCodes.Contains(g.Key)))
            rep.Items.Add(saved.Key + " — " + saved.First().RepresentativeName);
        rep.SelectedIndex = 0;
        foreach (var s in new[] { "All statuses", "Earned", "Approved", "Partially paid", "Paid" }) status.Items.Add(s);
        status.SelectedIndex = 0;
        filters.Children.Add(rep); filters.Children.Add(from); filters.Children.Add(to); filters.Children.Add(invoice);
        filters.Children.Add(source); filters.Children.Add(status); filters.Children.Add(payout);
        var export = new Button { Content = "Export CSV", Margin = new Thickness(3), Padding = new Thickness(8, 4, 8, 4) };
        var print = new Button { Content = "Print", Margin = new Thickness(3), Padding = new Thickness(8, 4, 8, 4) };
        filters.Children.Add(export); filters.Children.Add(print);
        top.Children.Add(filters); top.Children.Add(totals);
        DockPanel.SetDock(top, Dock.Top); root.Children.Add(top); root.Children.Add(grid); Content = root;
        rep.SelectionChanged += (_, _) => Refresh(); status.SelectionChanged += (_, _) => Refresh();
        from.SelectedDateChanged += (_, _) => Refresh(); to.SelectedDateChanged += (_, _) => Refresh();
        invoice.TextChanged += (_, _) => Refresh(); source.TextChanged += (_, _) => Refresh(); payout.TextChanged += (_, _) => Refresh();
        export.Click += (_, _) => Export();
        print.Click += (_, _) => PrintReport();
        Refresh();
    }

    private void Refresh()
    {
        var code = rep.SelectedIndex <= 0 ? null : rep.SelectedItem?.ToString()?.Split(" — ", 2)[0];
        rows = service.CommissionAccruals
            .Where(a => a is not null && (code is null || string.Equals(a.RepresentativeCode, code, StringComparison.OrdinalIgnoreCase)))
            .Select(a =>
            {
                var allocations = service.RepresentativePayouts.Where(p => p is not null)
                    .SelectMany(p => (p.Allocations ?? new List<CommissionPayoutAllocation>()).Where(x => x is not null && x.AccrualId == a.Id).Select(x => (Payout: p, Allocation: x))).ToList();
                var paidAmount = allocations.Sum(x => x.Allocation.Amount);
                var approval = service.CommissionApprovals.FirstOrDefault(x => x is not null && x.AccrualId == a.Id);
                var state = service.GetCommissionAccrualStatus(a).ToString();
                return new ReportRow(a.RepresentativeCode ?? "", a.RepresentativeName ?? "", a.CollectedAt.ToString("yyyy-MM-dd HH:mm"), (a.PaymentReceiptNumber ?? "") + " / " + (a.SourceAllocationId ?? ""),
                    a.InvoiceNumber ?? "", a.ProductCode ?? "", (a.RuleDescription ?? "") + " | " + (a.RuleScopeSnapshot ?? "") + " | " + a.Basis, a.RatePercent,
                    a.InvoiceEligibleBase, a.CollectedEligibleAmount, a.CommissionAmount, state, approval?.ApprovedBy ?? "", approval?.Date.ToString("yyyy-MM-dd HH:mm") ?? "",
                    paidAmount, string.Join(", ", allocations.Select(x => x.Payout.ReceiptNumber).Distinct()),
                    Math.Max(0, (approval is null ? 0 : a.CommissionAmount) - paidAmount), a.Id, a);
            })
            .Where(x => (from.SelectedDate is null || x.Accrual.CollectedAt.Date >= from.SelectedDate.Value.Date) && (to.SelectedDate is null || x.Accrual.CollectedAt.Date <= to.SelectedDate.Value.Date))
            .Where(x => string.IsNullOrWhiteSpace(invoice.Text) || x.Invoice.Contains(invoice.Text.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(x => string.IsNullOrWhiteSpace(source.Text) || x.SourceReference.Contains(source.Text.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(x => string.IsNullOrWhiteSpace(payout.Text) || x.PayoutReferences.Contains(payout.Text.Trim(), StringComparison.OrdinalIgnoreCase))
        .Where(x => status.SelectedIndex <= 0 || x.ApprovalStatus.Equals(status.SelectedItem?.ToString() switch { "Partially paid" => "PartiallyPaid", _ => status.SelectedItem?.ToString() }, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.Accrual.CollectedAt).ToList();
        grid.ItemsSource = rows.Select(x => new { x.RepresentativeCode, x.Representative, x.AccrualDate, x.SourceReference, x.Invoice, x.Product,
            x.RuleSnapshot, x.RatePercent, x.EligibleBasis, x.EligibleCollected, x.Commission, x.ApprovalStatus, x.Approver, x.ApprovalDate, x.Paid, x.PayoutReferences, x.Outstanding }).ToList();
        var earned = rows.Sum(x => x.Commission);
        var approved = rows.Where(x => x.ApprovalStatus != CommissionAccrualStatus.Earned.ToString()).Sum(x => x.Commission);
        var receiptCount = GetPayoutReceiptCount();
        totals.Text = $"Filtered totals — earned {earned:N2}; approved {approved:N2}; unapproved {earned - approved:N2}; allocated paid {rows.Sum(x => x.Paid):N2}; approved outstanding {rows.Sum(x => x.Outstanding):N2}. Accruals {rows.Count}; approvals {GetApprovalCount()}; payout receipts {receiptCount}.";
    }

    private void Export()
    {
        var dialog = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", DefaultExt = ".csv", FileName = "commission-report.csv" };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;
        File.WriteAllText(dialog.FileName, BuildCsv(), new UTF8Encoding(true));
    }

    private string BuildCsv()
    {
        static string Q(string? s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";
        var csv = new StringBuilder("Representative Code,Representative,Accrual Date,Source,Invoice,Product,Rule Snapshot,Rate %,Eligible Basis,Eligible Collected,Commission,Approved Commission,Unapproved Commission,Approval Status,Approver,Approval Date,Paid,Payout References,Outstanding,Accrual Count,Approval Count,Payout Receipt Count\r\n");
        foreach (var x in rows)
        {
            var fields = new[] { x.RepresentativeCode, x.Representative, x.AccrualDate, x.SourceReference, x.Invoice, x.Product, x.RuleSnapshot,
                x.RatePercent.ToString(CultureInfo.InvariantCulture), x.EligibleBasis.ToString(CultureInfo.InvariantCulture), x.EligibleCollected.ToString(CultureInfo.InvariantCulture), x.Commission.ToString(CultureInfo.InvariantCulture),
                IsApproved(x) ? x.Commission.ToString(CultureInfo.InvariantCulture) : "0", IsApproved(x) ? "0" : x.Commission.ToString(CultureInfo.InvariantCulture),
                x.ApprovalStatus, x.Approver, x.ApprovalDate, x.Paid.ToString(CultureInfo.InvariantCulture), x.PayoutReferences, x.Outstanding.ToString(CultureInfo.InvariantCulture), "", "", "" };
            csv.AppendLine(string.Join(",", fields.Select(Q)));
        }
        csv.AppendLine(string.Join(",", new[] { "TOTALS", "", "", "", "", "", "", "", "", rows.Sum(x => x.EligibleCollected).ToString(CultureInfo.InvariantCulture),
            rows.Sum(x => x.Commission).ToString(CultureInfo.InvariantCulture), rows.Where(IsApproved).Sum(x => x.Commission).ToString(CultureInfo.InvariantCulture),
            rows.Where(x => !IsApproved(x)).Sum(x => x.Commission).ToString(CultureInfo.InvariantCulture), "", "", "", rows.Sum(x => x.Paid).ToString(CultureInfo.InvariantCulture), "",
            rows.Sum(x => x.Outstanding).ToString(CultureInfo.InvariantCulture), rows.Count.ToString(CultureInfo.InvariantCulture),
            GetApprovalCount().ToString(CultureInfo.InvariantCulture), GetPayoutReceiptCount().ToString(CultureInfo.InvariantCulture) }.Select(Q)));
        return csv.ToString();
    }

    private static bool IsApproved(ReportRow row) => row.ApprovalStatus != CommissionAccrualStatus.Earned.ToString();

    private int GetPayoutReceiptCount() => rows.SelectMany(x => x.PayoutReferences.Split(',', StringSplitOptions.RemoveEmptyEntries))
        .Distinct(StringComparer.OrdinalIgnoreCase).Count();

    private int GetApprovalCount()
    {
        var filteredIds = rows.Select(x => x.AccrualId).ToHashSet(StringComparer.Ordinal);
        return service.CommissionApprovals.Count(x => x is not null && filteredIds.Contains(x.AccrualId ?? ""));
    }

    private void PrintReport()
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true) return;
        var document = new FlowDocument
        {
            PageWidth = dialog.PrintableAreaWidth,
            PageHeight = dialog.PrintableAreaHeight,
            PagePadding = new Thickness(24),
            FontSize = 8
        };
        document.Blocks.Add(new Paragraph(new Run("Commission report — filtered results")) { FontSize = 14, FontWeight = FontWeights.Bold });
        document.Blocks.Add(new Paragraph(new Run(totals.Text)));
        if (rows.Count == 0)
        {
            document.Blocks.Add(new Paragraph(new Run("No commission records match the selected filters.")));
        }
        else
        {
            var headers = new[] { "Rep", "Date", "Source", "Invoice", "Product", "Rate", "Basis", "Commission", "Status", "Approver", "Paid", "Payout refs", "Outstanding" };
            var table = new Table { CellSpacing = 0 };
            foreach (var _ in headers) table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
            var header = new TableRow();
            foreach (var value in headers) header.Cells.Add(Cell(value, true));
            var group = new TableRowGroup(); group.Rows.Add(header);
            foreach (var row in rows)
            {
                var values = new[] { row.RepresentativeCode, row.AccrualDate, row.SourceReference, row.Invoice, row.Product,
                    row.RatePercent.ToString("N2", CultureInfo.InvariantCulture), row.EligibleBasis.ToString("N2", CultureInfo.InvariantCulture),
                    row.Commission.ToString("N2", CultureInfo.InvariantCulture), row.ApprovalStatus, row.Approver,
                    row.Paid.ToString("N2", CultureInfo.InvariantCulture), row.PayoutReferences, row.Outstanding.ToString("N2", CultureInfo.InvariantCulture) };
                var tableRow = new TableRow();
                foreach (var value in values) tableRow.Cells.Add(Cell(value));
                group.Rows.Add(tableRow);
            }
            table.RowGroups.Add(group); document.Blocks.Add(table);
        }
        dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, "Commission report");

        static TableCell Cell(string value, bool bold = false)
        {
            var paragraph = new Paragraph(new Run(value ?? "")) { Margin = new Thickness(2) };
            if (bold) paragraph.FontWeight = FontWeights.Bold;
            return new TableCell(paragraph) { Padding = new Thickness(2), BorderThickness = new Thickness(0.5), BorderBrush = System.Windows.Media.Brushes.Gray };
        }
    }
}
