using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Win32;
using SmartErpDesktop.Models;
using SmartErpDesktop.Services;
using System.IO;

namespace SmartErpDesktop.Views;

public sealed class CommissionReportView : UserControl
{
    private sealed record ReportRow(
        string RepresentativeCode, string Representative, string AccrualDate, string SourceReference,
        string Invoice, string Product, string RuleSnapshot, decimal RatePercent, decimal EligibleBasis,
        decimal EligibleCollected, decimal Commission, string ApprovalStatus, string Approver,
        string ApprovalDate, decimal Paid, string PayoutReferences, decimal Outstanding,
        string AccrualId, CommissionAccrual Accrual);

    private readonly MockErpService service;
    private readonly ComboBox rep = new() { Width = 175, Margin = new Thickness(3), MinWidth = 160 };
    private readonly DatePicker from = new() { Width = 135, Margin = new Thickness(3) };
    private readonly DatePicker to = new() { Width = 135, Margin = new Thickness(3) };
    private readonly TextBox invoice = new() { Width = 130, Margin = new Thickness(3), ToolTip = "رقم الفاتورة" };
    private readonly TextBox source = new() { Width = 140, Margin = new Thickness(3), ToolTip = "رقم إيصال التحصيل أو مصدر الاستحقاق" };
    private readonly TextBox payout = new() { Width = 130, Margin = new Thickness(3), ToolTip = "رقم إيصال صرف العمولة" };
    private readonly ComboBox status = new() { Width = 130, Margin = new Thickness(3) };
    private readonly DataGrid grid = new()
    {
        IsReadOnly = true,
        CanUserAddRows = false,
        AutoGenerateColumns = false,
        Margin = new Thickness(3),
        FlowDirection = FlowDirection.RightToLeft,
        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto
    };
    private readonly TextBlock totals = new()
    {
        Margin = new Thickness(6),
        FontWeight = FontWeights.SemiBold,
        TextWrapping = TextWrapping.Wrap,
        TextAlignment = TextAlignment.Right
    };
    private List<ReportRow> rows = new();

    private static readonly (string Label, string Value)[] StatusOptions =
    {
        ("كل الحالات", ""), ("مستحقة", "Earned"), ("معتمدة", "Approved"),
        ("مدفوعة جزئيًا", "PartiallyPaid"), ("مصروفة بالكامل", "Paid")
    };

    public CommissionReportView(MockErpService service)
    {
        this.service = service;

        var root = new DockPanel { Margin = new Thickness(8), FlowDirection = FlowDirection.RightToLeft };
        var top = new StackPanel();
        var filters = new WrapPanel { FlowDirection = FlowDirection.RightToLeft };

        rep.Items.Add("كل المندوبين");
        foreach (var r in service.Representatives.OrderBy(x => x.Code))
            rep.Items.Add(r.Code + " — " + r.Name);

        var knownCodes = service.Representatives.Select(x => x.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var saved in service.CommissionAccruals
                     .Where(x => x is not null && !string.IsNullOrWhiteSpace(x.RepresentativeCode))
                     .GroupBy(x => x.RepresentativeCode, StringComparer.OrdinalIgnoreCase)
                     .Where(g => !knownCodes.Contains(g.Key)))
            rep.Items.Add(saved.Key + " — " + saved.First().RepresentativeName);
        rep.SelectedIndex = 0;

        foreach (var option in StatusOptions)
            status.Items.Add(option.Label);
        status.SelectedIndex = 0;

        from.ToolTip = "من تاريخ";
        to.ToolTip = "إلى تاريخ";
        invoice.ToolTip = "رقم الفاتورة";
        source.ToolTip = "رقم إيصال التحصيل أو مصدر الاستحقاق";
        payout.ToolTip = "رقم إيصال صرف العمولة";

        filters.Children.Add(rep);
        filters.Children.Add(from);
        filters.Children.Add(to);
        filters.Children.Add(invoice);
        filters.Children.Add(source);
        filters.Children.Add(status);
        filters.Children.Add(payout);

        var export = new Button
        {
            Content = "تصدير CSV",
            Margin = new Thickness(3),
            Padding = new Thickness(8, 4, 8, 4)
        };
        var print = new Button
        {
            Content = "طباعة",
            Margin = new Thickness(3),
            Padding = new Thickness(8, 4, 8, 4)
        };
        filters.Children.Add(export);
        filters.Children.Add(print);

        ConfigureGrid();
        top.Children.Add(filters);
        top.Children.Add(totals);
        DockPanel.SetDock(top, Dock.Top);
        root.Children.Add(top);
        root.Children.Add(grid);
        Content = root;

        rep.SelectionChanged += (_, _) => Refresh();
        status.SelectionChanged += (_, _) => Refresh();
        from.SelectedDateChanged += (_, _) => Refresh();
        to.SelectedDateChanged += (_, _) => Refresh();
        invoice.TextChanged += (_, _) => Refresh();
        source.TextChanged += (_, _) => Refresh();
        payout.TextChanged += (_, _) => Refresh();
        export.Click += (_, _) => Export();
        print.Click += (_, _) => PrintReport();

        Refresh();
    }

    private void ConfigureGrid()
    {
        AddColumn("كود المندوب", nameof(ReportRow.RepresentativeCode), 105);
        AddColumn("المندوب", nameof(ReportRow.Representative), 140);
        AddColumn("تاريخ الاستحقاق", nameof(ReportRow.AccrualDate), 135);
        AddColumn("مصدر الاستحقاق", nameof(ReportRow.SourceReference), 220);
        AddColumn("رقم الفاتورة", nameof(ReportRow.Invoice), 105);
        AddColumn("الصنف", nameof(ReportRow.Product), 90);
        AddColumn("بيانات قاعدة العمولة", nameof(ReportRow.RuleSnapshot), 260);
        AddColumn("النسبة %", nameof(ReportRow.RatePercent), 85);
        AddColumn("أساس العمولة", nameof(ReportRow.EligibleBasis), 105);
        AddColumn("المبلغ المحصل المؤهل", nameof(ReportRow.EligibleCollected), 130);
        AddColumn("قيمة العمولة", nameof(ReportRow.Commission), 100);
        AddColumn("الحالة", nameof(ReportRow.ApprovalStatus), 100);
        AddColumn("اعتمدها", nameof(ReportRow.Approver), 100);
        AddColumn("تاريخ الاعتماد", nameof(ReportRow.ApprovalDate), 130);
        AddColumn("المصروف", nameof(ReportRow.Paid), 90);
        AddColumn("إيصالات الصرف", nameof(ReportRow.PayoutReferences), 150);
        AddColumn("المتبقي المعتمد", nameof(ReportRow.Outstanding), 115);
    }

    private void AddColumn(string header, string bindingPath, double width)
    {
        grid.Columns.Add(new DataGridTextColumn
        {
            Header = header,
            Binding = new Binding(bindingPath),
            Width = new DataGridLength(width),
            CanUserSort = true
        });
    }

    private void Refresh()
    {
        var code = rep.SelectedIndex <= 0
            ? null
            : rep.SelectedItem?.ToString()?.Split(" — ", 2)[0];

        rows = service.CommissionAccruals
            .Where(a => a is not null &&
                        (code is null || string.Equals(a.RepresentativeCode, code, StringComparison.OrdinalIgnoreCase)))
            .Select(a =>
            {
                var allocations = service.RepresentativePayouts.Where(p => p is not null)
                    .SelectMany(p => (p.Allocations ?? new List<CommissionPayoutAllocation>())
                        .Where(x => x is not null && x.AccrualId == a.Id)
                        .Select(x => (Payout: p, Allocation: x)))
                    .ToList();

                var paidAmount = allocations.Sum(x => x.Allocation.Amount);
                var approval = service.CommissionApprovals.FirstOrDefault(x => x is not null && x.AccrualId == a.Id);
                var state = service.GetCommissionAccrualStatus(a).ToString();
                return new ReportRow(
                    a.RepresentativeCode ?? "",
                    a.RepresentativeName ?? "",
                    a.CollectedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                    (a.PaymentReceiptNumber ?? "") + " / " + (a.SourceAllocationId ?? ""),
                    a.InvoiceNumber ?? "",
                    a.ProductCode ?? "",
                    (a.RuleDescription ?? "") + " | " + (a.RuleScopeSnapshot ?? "") + " | " + a.Basis,
                    a.RatePercent, a.InvoiceEligibleBase, a.CollectedEligibleAmount, a.CommissionAmount,
                    TranslateStatus(state), approval?.ApprovedBy ?? "",
                    approval?.Date.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? "",
                    paidAmount,
                    string.Join(", ", allocations.Select(x => x.Payout.ReceiptNumber).Distinct()),
                    Math.Max(0, (approval is null ? 0 : a.CommissionAmount) - paidAmount),
                    a.Id, a);
            })
            .Where(x => from.SelectedDate is null || x.Accrual.CollectedAt.Date >= from.SelectedDate.Value.Date)
            .Where(x => to.SelectedDate is null || x.Accrual.CollectedAt.Date <= to.SelectedDate.Value.Date)
            .Where(x => string.IsNullOrWhiteSpace(invoice.Text) ||
                        x.Invoice.Contains(invoice.Text.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(x => string.IsNullOrWhiteSpace(source.Text) ||
                        x.SourceReference.Contains(source.Text.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(x => string.IsNullOrWhiteSpace(payout.Text) ||
                        x.PayoutReferences.Contains(payout.Text.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(x => StatusMatches(x.AccrualId, status.SelectedIndex))
            .OrderByDescending(x => x.Accrual.CollectedAt)
            .ToList();

        grid.ItemsSource = rows;
        var earned = rows.Sum(x => x.Commission);
        var approved = rows.Where(IsApproved).Sum(x => x.Commission);
        totals.Text =
            $"إجمالي النتائج المفلترة — المستحق: {earned:N2}؛ المعتمد: {approved:N2}؛ غير المعتمد: {earned - approved:N2}؛ المصروف: {rows.Sum(x => x.Paid):N2}؛ المتبقي المعتمد: {rows.Sum(x => x.Outstanding):N2}. " +
            $"عدد الاستحقاقات: {rows.Count}؛ عدد عمليات الاعتماد: {GetApprovalCount()}؛ عدد إيصالات الصرف: {GetPayoutReceiptCount()}.";
    }

    private bool StatusMatches(string accrualId, int selectedIndex)
    {
        if (selectedIndex <= 0) return true;
        var accrual = rows.FirstOrDefault(x => x.AccrualId == accrualId);
        // The current row may not yet be in rows while the query is being evaluated,
        // so derive the status directly from the persisted accrual and service.
        var sourceAccrual = service.CommissionAccruals.FirstOrDefault(x => x is not null && x.Id == accrualId);
        if (sourceAccrual is null) return false;
        var state = service.GetCommissionAccrualStatus(sourceAccrual).ToString();
        var wanted = StatusOptions[selectedIndex].Value;
        return string.Equals(state, wanted, StringComparison.OrdinalIgnoreCase);
    }

    private static string TranslateStatus(string statusValue) => statusValue switch
    {
        "Earned" => "مستحقة",
        "Approved" => "معتمدة",
        "PartiallyPaid" => "مدفوعة جزئيًا",
        "Paid" => "مصروفة بالكامل",
        _ => statusValue
    };

    private static bool IsApproved(ReportRow row) => row.ApprovalStatus != "مستحقة";

    private int GetPayoutReceiptCount() => rows
        .SelectMany(x => x.PayoutReferences.Split(',', StringSplitOptions.RemoveEmptyEntries))
        .Distinct(StringComparer.OrdinalIgnoreCase).Count();

    private int GetApprovalCount()
    {
        var filteredIds = rows.Select(x => x.AccrualId).ToHashSet(StringComparer.Ordinal);
        return service.CommissionApprovals.Count(x => x is not null && filteredIds.Contains(x.AccrualId ?? ""));
    }

    private void Export()
    {
        var dialog = new SaveFileDialog
        {
            Title = "تصدير تقرير العمولات",
            Filter = "ملفات CSV (*.csv)|*.csv|كل الملفات (*.*)|*.*",
            DefaultExt = ".csv",
            FileName = "تقرير-العمولات.csv"
        };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;
        File.WriteAllText(dialog.FileName, BuildCsv(), new UTF8Encoding(true));
        MessageBox.Show(Window.GetWindow(this), "تم تصدير تقرير العمولات بنجاح.",
            "تصدير التقرير", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private string BuildCsv()
    {
        static string Q(string? value) => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
        var csv = new StringBuilder();
        csv.AppendLine(string.Join(",", new[]
        {
            "كود المندوب", "المندوب", "تاريخ الاستحقاق", "مصدر الاستحقاق", "رقم الفاتورة", "الصنف",
            "بيانات قاعدة العمولة", "النسبة %", "أساس العمولة", "المبلغ المحصل المؤهل", "قيمة العمولة",
            "العمولة المعتمدة", "العمولة غير المعتمدة", "الحالة", "اعتمدها", "تاريخ الاعتماد",
            "المصروف", "إيصالات الصرف", "المتبقي المعتمد", "عدد الاستحقاقات", "عدد عمليات الاعتماد", "عدد إيصالات الصرف"
        }.Select(Q)));

        foreach (var x in rows)
        {
            var fields = new[]
            {
                x.RepresentativeCode, x.Representative, x.AccrualDate, x.SourceReference, x.Invoice, x.Product,
                x.RuleSnapshot, x.RatePercent.ToString(CultureInfo.InvariantCulture),
                x.EligibleBasis.ToString(CultureInfo.InvariantCulture),
                x.EligibleCollected.ToString(CultureInfo.InvariantCulture),
                x.Commission.ToString(CultureInfo.InvariantCulture),
                IsApproved(x) ? x.Commission.ToString(CultureInfo.InvariantCulture) : "0",
                IsApproved(x) ? "0" : x.Commission.ToString(CultureInfo.InvariantCulture),
                x.ApprovalStatus, x.Approver, x.ApprovalDate,
                x.Paid.ToString(CultureInfo.InvariantCulture), x.PayoutReferences,
                x.Outstanding.ToString(CultureInfo.InvariantCulture), "", "", ""
            };
            csv.AppendLine(string.Join(",", fields.Select(Q)));
        }

        csv.AppendLine(string.Join(",", new[]
        {
            "الإجمالي", "", "", "", "", "", "", "", "",
            rows.Sum(x => x.EligibleCollected).ToString(CultureInfo.InvariantCulture),
            rows.Sum(x => x.Commission).ToString(CultureInfo.InvariantCulture),
            rows.Where(IsApproved).Sum(x => x.Commission).ToString(CultureInfo.InvariantCulture),
            rows.Where(x => !IsApproved(x)).Sum(x => x.Commission).ToString(CultureInfo.InvariantCulture),
            "", "", "", rows.Sum(x => x.Paid).ToString(CultureInfo.InvariantCulture), "",
            rows.Sum(x => x.Outstanding).ToString(CultureInfo.InvariantCulture),
            rows.Count.ToString(CultureInfo.InvariantCulture),
            GetApprovalCount().ToString(CultureInfo.InvariantCulture),
            GetPayoutReceiptCount().ToString(CultureInfo.InvariantCulture)
        }.Select(Q)));

        return csv.ToString();
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
            FontSize = 8,
            FlowDirection = FlowDirection.RightToLeft,
            Language = System.Windows.Markup.XmlLanguage.GetLanguage("ar-EG")
        };
        document.Blocks.Add(new Paragraph(new Run("تقرير العمولات — النتائج المفلترة"))
        {
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Right
        });
        document.Blocks.Add(new Paragraph(new Run(totals.Text)) { TextAlignment = TextAlignment.Right });

        if (rows.Count == 0)
        {
            document.Blocks.Add(new Paragraph(new Run("لا توجد سجلات عمولات مطابقة للفلاتر المحددة."))
            {
                TextAlignment = TextAlignment.Right
            });
        }
        else
        {
            var headers = new[]
            {
                "المندوب", "التاريخ", "مصدر الاستحقاق", "الفاتورة", "الصنف", "النسبة",
                "أساس العمولة", "قيمة العمولة", "الحالة", "اعتمدها", "المصروف",
                "إيصالات الصرف", "المتبقي المعتمد"
            };
            var table = new Table { CellSpacing = 0 };
            foreach (var _ in headers)
                table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

            var header = new TableRow();
            foreach (var value in headers) header.Cells.Add(Cell(value, true));
            var group = new TableRowGroup();
            group.Rows.Add(header);

            foreach (var row in rows)
            {
                var values = new[]
                {
                    row.Representative + " (" + row.RepresentativeCode + ")",
                    row.AccrualDate, row.SourceReference, row.Invoice, row.Product,
                    row.RatePercent.ToString("N2", CultureInfo.InvariantCulture),
                    row.EligibleBasis.ToString("N2", CultureInfo.InvariantCulture),
                    row.Commission.ToString("N2", CultureInfo.InvariantCulture),
                    row.ApprovalStatus, row.Approver,
                    row.Paid.ToString("N2", CultureInfo.InvariantCulture),
                    row.PayoutReferences,
                    row.Outstanding.ToString("N2", CultureInfo.InvariantCulture)
                };
                var tableRow = new TableRow();
                foreach (var value in values) tableRow.Cells.Add(Cell(value));
                group.Rows.Add(tableRow);
            }

            table.RowGroups.Add(group);
            document.Blocks.Add(table);
        }

        dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, "تقرير العمولات");

        static TableCell Cell(string value, bool bold = false)
        {
            var paragraph = new Paragraph(new Run(value ?? ""))
            {
                Margin = new Thickness(2),
                TextAlignment = TextAlignment.Right
            };
            if (bold) paragraph.FontWeight = FontWeights.Bold;
            return new TableCell(paragraph)
            {
                Padding = new Thickness(2),
                BorderThickness = new Thickness(0.5),
                BorderBrush = Brushes.Gray
            };
        }
    }
}
