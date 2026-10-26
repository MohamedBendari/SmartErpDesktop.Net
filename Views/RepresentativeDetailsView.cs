using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Win32;
using System.Globalization;
using System.Text;
using SmartErpDesktop.Models;
using SmartErpDesktop.Services;

namespace SmartErpDesktop.Views;

public sealed class RepresentativeDetailsView : UserControl
{
    private sealed record InvoiceRow(string Number, string Date, string Customer, string Type, decimal Subtotal, decimal Discount, decimal NetSales, decimal Paid, decimal Remaining, string Status);
    private sealed record CollectionRow(string Receipt, string Date, string Invoice, decimal Amount, string Method, string Note);
    private sealed record InstallmentRow(string Invoice, int Sequence, string DueDate, decimal Amount, decimal Paid, decimal Remaining, string Status);
    private sealed record CommissionRow(string Id, string Invoice, string Product, string PaymentReceipt, string Date, string Rule, decimal Rate, decimal EligibleBase, decimal CollectedEligible, decimal Amount, decimal Paid, string Status, string ApprovedBy, string ApprovedAt, CommissionAccrual Accrual);
    private sealed record PayoutRow(string Receipt, string Date, decimal Amount, string Method, string Notes, RepresentativePayout Payout);
    private sealed record CommissionStatementRow(string Date, string Type, string Reference, decimal Earned, decimal Paid, string Status);

    private readonly MockErpService service;
    private readonly Representative representative;
    private readonly List<Invoice> representativeInvoices;
    private readonly DataGrid invoicesGrid = new() { IsReadOnly = true, CanUserAddRows = false, AutoGenerateColumns = false };
    private readonly DatePicker fromDate = new() { Width = 150, Margin = new Thickness(4) };
    private readonly DatePicker toDate = new() { Width = 150, Margin = new Thickness(4) };
    private readonly TextBox invoiceFilter = new() { Width = 220, Margin = new Thickness(4), ToolTip = "بحث برقم الفاتورة أو العميل أو النوع" };
    private readonly TextBlock invoiceSummary = new() { Margin = new Thickness(8), FontWeight = FontWeights.SemiBold };
    private Action refreshCommissionAccount = () => { };

    public RepresentativeDetailsView(MockErpService service, Representative representative, Action back)
    {
        this.service = service;
        this.representative = representative;
        representativeInvoices = service.Invoices
            .Where(i => i.RepresentativeCode.Equals(representative.Code, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(i => i.Date)
            .ThenBy(i => i.Number, StringComparer.Ordinal)
            .ToList();

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var header = new DockPanel { Margin = new Thickness(3, 3, 3, 10) };
        var backButton = new Button { Content = "العودة إلى المندوبين", Padding = new Thickness(14, 8, 14, 8), Background = new SolidColorBrush(Color.FromRgb(69, 85, 109)) };
        backButton.Click += (_, _) => back();
        DockPanel.SetDock(backButton, Dock.Left);
        header.Children.Add(backButton);
        var title = new StackPanel();
        title.Children.Add(new TextBlock { Text = representative.Name, FontSize = 22, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(18, 32, 56)) });
        title.Children.Add(new TextBlock { Text = $"كود المندوب: {representative.Code} | الحالة: {(representative.IsActive ? "نشط" : "غير نشط")}", Foreground = new SolidColorBrush(Color.FromRgb(107, 119, 140)) });
        header.Children.Add(title);
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        var tabs = new TabControl { Margin = new Thickness(3) };
        tabs.Items.Add(new TabItem { Header = "نظرة عامة", Content = BuildOverview() });
        tabs.Items.Add(new TabItem { Header = "الفواتير", Content = BuildInvoices() });
        tabs.Items.Add(new TabItem { Header = "التحصيلات والأقساط", Content = BuildCollections() });
        tabs.Items.Add(new TabItem { Header = "العمولات", Content = BuildCommissions() });
        tabs.Items.Add(new TabItem { Header = "تقرير العمولات", Content = new CommissionReportView(service) });
        tabs.Items.Add(new TabItem { Header = "مدفوعات العمولات", Content = BuildPayouts() });
        var commissionAccountTab = new TabItem { Header = "كشف الحساب", Content = BuildCommissionAccount() };
        tabs.Items.Add(commissionAccountTab);
        tabs.SelectionChanged += (_, args) =>
        {
            if (ReferenceEquals(args.Source, tabs) && ReferenceEquals(tabs.SelectedItem, commissionAccountTab))
                refreshCommissionAccount();
        };
        Grid.SetRow(tabs, 1);
        root.Children.Add(tabs);
        Content = root;
    }

    private UIElement BuildOverview()
    {
        var content = new StackPanel { Margin = new Thickness(16) };
        AddSection(content, "بيانات المندوب");
        AddInfo(content, "الكود", representative.Code);
        AddInfo(content, "الاسم", representative.Name);
        AddInfo(content, "الهاتف", representative.Phone);
        AddInfo(content, "العنوان", representative.Address);
        AddInfo(content, "الحالة", representative.IsActive ? "نشط" : "غير نشط");
        AddInfo(content, "ملاحظات", string.IsNullOrWhiteSpace(representative.Notes) ? "—" : representative.Notes);

        var invoiceTotal = representativeInvoices.Sum(i => i.Total);
        var grossSales = representativeInvoices.Sum(i => i.Lines.Sum(line => line.Quantity * line.UnitPrice));
        var discounts = representativeInvoices.Sum(GetInvoiceDiscount);
        var collected = GetLinkedCollections().Sum(x => x.Amount);
        var outstanding = representativeInvoices.Sum(i => i.Remaining);
        AddSection(content, "مؤشرات المبيعات والتحصيل");
        AddInfo(content, "عدد الفواتير", representativeInvoices.Count.ToString());
        AddInfo(content, "إجمالي الفواتير قبل الخصم", $"{grossSales:N2} ج.م");
        AddInfo(content, "إجمالي الخصومات", $"{discounts:N2} ج.م");
        AddInfo(content, "صافي المبيعات", $"{invoiceTotal:N2} ج.م");
        AddInfo(content, "التحصيلات المرتبطة صراحةً بفواتير المندوب", $"{collected:N2} ج.م");
        AddInfo(content, "المتبقي على فواتير المندوب", $"{outstanding:N2} ج.م");
        content.Children.Add(new TextBlock
        {
            Text = "صافي المبيعات هو إجمالي الفواتير بعد الخصومات. التحصيلات تعرض فقط الدفعات التي تحتوي على ربط محفوظ برقم الفاتورة؛ التحصيلات التاريخية غير المرتبطة لا تُنسب للمندوب.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.DimGray,
            Margin = new Thickness(0, 12, 0, 0)
        });
        return new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Background = Brushes.White };
    }

    private UIElement BuildInvoices()
    {
        var root = new DockPanel { LastChildFill = true, Margin = new Thickness(12) };
        var filters = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        filters.Children.Add(new TextBlock { Text = "من تاريخ", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4) });
        filters.Children.Add(fromDate);
        filters.Children.Add(new TextBlock { Text = "إلى تاريخ", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4) });
        filters.Children.Add(toDate);
        filters.Children.Add(invoiceFilter);
        var apply = new Button { Content = "تصفية", Padding = new Thickness(12, 6, 12, 6), Margin = new Thickness(4) };
        apply.Click += (_, _) => RefreshInvoices();
        filters.Children.Add(apply);
        DockPanel.SetDock(filters, Dock.Top);
        root.Children.Add(filters);
        var summaryPanel = new StackPanel();
        summaryPanel.Children.Add(invoiceSummary);
        DockPanel.SetDock(summaryPanel, Dock.Bottom);
        root.Children.Add(summaryPanel);
        AddColumns(invoicesGrid,
            ("رقم الفاتورة", nameof(InvoiceRow.Number), 1), ("التاريخ", nameof(InvoiceRow.Date), 1.3),
            ("العميل", nameof(InvoiceRow.Customer), 1.5), ("نوع البيع", nameof(InvoiceRow.Type), 1),
            ("قبل الخصم", nameof(InvoiceRow.Subtotal), 1), ("الخصومات", nameof(InvoiceRow.Discount), 1),
            ("صافي الفاتورة", nameof(InvoiceRow.NetSales), 1), ("المدفوع", nameof(InvoiceRow.Paid), 1),
            ("المتبقي", nameof(InvoiceRow.Remaining), 1), ("الحالة", nameof(InvoiceRow.Status), 1));
        root.Children.Add(invoicesGrid);
        RefreshInvoices();
        return root;
    }

    private void RefreshInvoices()
    {
        var query = invoiceFilter.Text.Trim();
        var rows = representativeInvoices
            .Where(i => (!fromDate.SelectedDate.HasValue || i.Date.Date >= fromDate.SelectedDate.Value.Date) &&
                        (!toDate.SelectedDate.HasValue || i.Date.Date <= toDate.SelectedDate.Value.Date) &&
                        (query.Length == 0 || i.Number.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                         i.CustomerName.Contains(query, StringComparison.OrdinalIgnoreCase) || i.Type.Contains(query, StringComparison.OrdinalIgnoreCase)))
            .Select(i => new InvoiceRow(i.Number, i.Date.ToString("yyyy/MM/dd HH:mm"), i.CustomerName, i.Type,
                i.Lines.Sum(l => l.Quantity * l.UnitPrice), GetInvoiceDiscount(i), i.Total, i.Paid, i.Remaining, i.Status))
            .ToList();
        invoicesGrid.ItemsSource = rows;
        invoiceSummary.Text = $"الفواتير المعروضة: {rows.Count} | إجمالي الفواتير: {rows.Sum(x => x.NetSales):N2} ج.م | الخصومات: {rows.Sum(x => x.Discount):N2} ج.م | المدفوع بالفواتير: {rows.Sum(x => x.Paid):N2} ج.م | المتبقي: {rows.Sum(x => x.Remaining):N2} ج.م";
    }

    private UIElement BuildCollections()
    {
        var root = new Grid { Margin = new Thickness(12) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var note = new TextBlock
        {
            Text = "تعرض التحصيلات المرتبطة صراحةً بفواتير المندوب فقط. التحصيلات القديمة التي لا تحتوي على ربط محفوظ بفاتورة لا يمكن نسبتها لهذا المندوب.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.DimGray,
            Margin = new Thickness(4, 4, 4, 8)
        };
        Grid.SetRow(note, 0); root.Children.Add(note);

        var collections = GetLinkedCollections();
        var collectionGrid = new DataGrid { IsReadOnly = true, CanUserAddRows = false, AutoGenerateColumns = false };
        AddColumns(collectionGrid, ("الإيصال", nameof(CollectionRow.Receipt), 1), ("التاريخ", nameof(CollectionRow.Date), 1.2),
            ("الفاتورة", nameof(CollectionRow.Invoice), 1), ("المبلغ المحصل", nameof(CollectionRow.Amount), 1),
            ("طريقة الدفع", nameof(CollectionRow.Method), 1), ("ملاحظات", nameof(CollectionRow.Note), 2));
        collectionGrid.ItemsSource = collections;
        Grid.SetRow(collectionGrid, 1); root.Children.Add(collectionGrid);
        var collectionsSummary = new TextBlock { Text = $"عدد قيود التحصيل المرتبطة: {collections.Count} | إجمالي التحصيل المرتبط: {collections.Sum(x => x.Amount):N2} ج.م", FontWeight = FontWeights.SemiBold, Margin = new Thickness(6, 8, 6, 10) };
        Grid.SetRow(collectionsSummary, 2); root.Children.Add(collectionsSummary);

        var installmentGrid = new DataGrid { IsReadOnly = true, CanUserAddRows = false, AutoGenerateColumns = false };
        AddColumns(installmentGrid, ("الفاتورة", nameof(InstallmentRow.Invoice), 1), ("رقم القسط", nameof(InstallmentRow.Sequence), 1),
            ("الاستحقاق", nameof(InstallmentRow.DueDate), 1.2), ("قيمة القسط", nameof(InstallmentRow.Amount), 1),
            ("المدفوع للقسط", nameof(InstallmentRow.Paid), 1), ("المتبقي", nameof(InstallmentRow.Remaining), 1), ("الحالة", nameof(InstallmentRow.Status), 1));
        var installments = service.Installments
            .Where(i => representativeInvoices.Any(invoice => invoice.Number.Equals(i.InvoiceNumber, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(i => i.DueDate).ThenBy(i => i.Sequence)
            .Select(i => new InstallmentRow(i.InvoiceNumber, i.Sequence, i.DueDate.ToString("yyyy/MM/dd"), i.Amount, i.PaidAmount, i.Remaining, i.Status))
            .ToList();
        installmentGrid.ItemsSource = installments;
        Grid.SetRow(installmentGrid, 3); root.Children.Add(installmentGrid);
        var installmentSummary = new TextBlock { Text = $"عدد الأقساط: {installments.Count} | إجمالي قيم الأقساط: {installments.Sum(x => x.Amount):N2} ج.م | المدفوع للأقساط: {installments.Sum(x => x.Paid):N2} ج.م | المتبقي: {installments.Sum(x => x.Remaining):N2} ج.م", FontWeight = FontWeights.SemiBold, Margin = new Thickness(6, 8, 6, 4) };
        Grid.SetRow(installmentSummary, 4); root.Children.Add(installmentSummary);
        return root;
    }

    private UIElement BuildCommissions()
    {
        var root = new DockPanel { Margin = new Thickness(12) };
        var controls = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        var approver = new TextBox { Width = 190, ToolTip = "اسم المسؤول عن الاعتماد" };
        var approve = new Button { Content = "اعتماد العمولات المستحقة المحددة" };
        controls.Children.Add(approver); controls.Children.Add(approve);
        var note = new TextBlock { Text = "تُحتسب العمولات من تخصيصات التحصيل المرتبطة بالفواتير. ولأن التحصيل يُسجل على مستوى الفاتورة، يُوزع مبلغ كل تخصيص تناسبياً على قيم السطور بعد خصم خصم السطر. لا يدخل خصم الفاتورة العام في أساس العمولة وفقاً للسياسة الحالية. ولا تستحق السطور التي لا تنطبق عليها قاعدة عمولة أي عمولة.", TextWrapping = TextWrapping.Wrap, Foreground = Brushes.DimGray, Margin = new Thickness(4) };
        var top = new StackPanel();
        var summary = new TextBlock { FontWeight = FontWeights.SemiBold, Margin = new Thickness(4) };
        top.Children.Add(summary);
        top.Children.Add(note); top.Children.Add(controls);
        DockPanel.SetDock(top, Dock.Top); root.Children.Add(top);
        var grid = new DataGrid { IsReadOnly = true, CanUserAddRows = false, AutoGenerateColumns = false, SelectionMode = DataGridSelectionMode.Extended };
        AddColumns(grid,
            ("رقم الاستحقاق", nameof(CommissionRow.Id), 1.3), ("رقم الفاتورة", nameof(CommissionRow.Invoice), 1), ("الصنف", nameof(CommissionRow.Product), 1),
            ("إيصال التحصيل", nameof(CommissionRow.PaymentReceipt), 1.2), ("تاريخ التحصيل", nameof(CommissionRow.Date), 1.4),
            ("بيانات القاعدة وقت الاستحقاق", nameof(CommissionRow.Rule), 2.4), ("النسبة %", nameof(CommissionRow.Rate), 0.8),
            ("أساس العمولة للسطر", nameof(CommissionRow.EligibleBase), 1), ("المبلغ المحصل المؤهل", nameof(CommissionRow.CollectedEligible), 1.2),
            ("العمولة المستحقة", nameof(CommissionRow.Amount), 1), ("المصروف", nameof(CommissionRow.Paid), 1),
            ("الحالة", nameof(CommissionRow.Status), 1), ("اعتمدها", nameof(CommissionRow.ApprovedBy), 1), ("تاريخ الاعتماد", nameof(CommissionRow.ApprovedAt), 1.4));
        void Refresh()
        {
            summary.Text = $"إجمالي العمولات المستحقة: {service.GetAccruedCommission(representative.Code):N2} | المعتمدة: {service.GetApprovedCommission(representative.Code):N2} | المصروفة: {service.GetPaidCommission(representative.Code):N2} | المتبقي المعتمد للصرف: {service.GetOutstandingCommission(representative.Code):N2}";
            grid.ItemsSource = service.CommissionAccruals.Where(x => x.RepresentativeCode.Equals(representative.Code, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.CollectedAt).Select(x => new CommissionRow(x.Id, x.InvoiceNumber, x.ProductCode, x.PaymentReceiptNumber,
                    x.CollectedAt.ToString("yyyy/MM/dd HH:mm"), x.RuleDescription + " | " + x.RuleScopeSnapshot, x.RatePercent, x.InvoiceEligibleBase, x.CollectedEligibleAmount,
                    x.CommissionAmount, service.RepresentativePayouts.SelectMany(p => p.Allocations).Where(a => a.AccrualId == x.Id).Sum(a => a.Amount),
                    GetAccrualStatusArabic(service.GetCommissionAccrualStatus(x)), service.CommissionApprovals.FirstOrDefault(a => a.AccrualId == x.Id)?.ApprovedBy ?? "—", service.CommissionApprovals.FirstOrDefault(a => a.AccrualId == x.Id)?.Date.ToString("yyyy/MM/dd HH:mm") ?? "—", x)).ToList();
        }
        approve.Click += (_, _) =>
        {
            try
            {
                service.ApproveCommissionAccruals(grid.SelectedItems.OfType<CommissionRow>().Select(x => x.Id), approver.Text);
                Refresh();
                refreshCommissionAccount();
            }
            catch (Exception ex) { MessageBox.Show(ArabicUi.ErrorMessage(ex, "تعذر اعتماد العمولات المحددة. تحقق من البيانات وحاول مرة أخرى."), "اعتماد العمولات", MessageBoxButton.OK, MessageBoxImage.Warning); }
        };
        Refresh(); root.Children.Add(grid);
        return root;
    }

    private UIElement BuildPayouts()
    {
        var root = new DockPanel { Margin = new Thickness(12) };
        var top = new StackPanel();
        var summary = new TextBlock { FontWeight = FontWeights.SemiBold, Margin = new Thickness(4) };
        var controls = new StackPanel { Orientation = Orientation.Horizontal };
        var create = new Button { Content = "تسجيل صرف" };
        var print = new Button { Content = "معاينة / طباعة إيصال الصرف", Background = new SolidColorBrush(Color.FromRgb(69, 85, 109)) };
        controls.Children.Add(create); controls.Children.Add(print);
        top.Children.Add(summary); top.Children.Add(controls); DockPanel.SetDock(top, Dock.Top); root.Children.Add(top);
        var grid = new DataGrid { IsReadOnly = true, CanUserAddRows = false, AutoGenerateColumns = false };
        AddColumns(grid, ("رقم الإيصال", nameof(PayoutRow.Receipt), 1.2), ("التاريخ", nameof(PayoutRow.Date), 1.4),
            ("المبلغ", nameof(PayoutRow.Amount), 1), ("طريقة الدفع", nameof(PayoutRow.Method), 1), ("ملاحظات", nameof(PayoutRow.Notes), 2));
        void Refresh()
        {
            summary.Text = $"المستحقة {service.GetAccruedCommission(representative.Code):N2} | المعتمدة {service.GetApprovedCommission(representative.Code):N2} | المصروفة {service.GetPaidCommission(representative.Code):N2} | المتبقي للصرف {service.GetOutstandingCommission(representative.Code):N2}";
            grid.ItemsSource = service.RepresentativePayouts.Where(x => x.RepresentativeCode.Equals(representative.Code, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.Date).Select(x => new PayoutRow(x.ReceiptNumber, x.Date.ToString("yyyy/MM/dd HH:mm"), x.Amount, x.Method, x.Notes, x)).ToList();
        }
        void PrintSelected()
        {
            if (grid.SelectedItem is not PayoutRow row) { MessageBox.Show("حدد إيصال صرف أولاً."); return; }
            try
            {
                var settings = ((App)Application.Current).CompanySettings.GetSettings();
                var doc = CommissionPayoutReceiptDocument.Create(row.Payout, service);
                ErpPrintDocument.CreatePreviewWindow(doc, "إيصال صرف عمولة مندوب", row.Payout.ReceiptNumber, settings, Window.GetWindow(this)).ShowDialog();
            }
            catch (Exception ex) { MessageBox.Show(ArabicUi.ErrorMessage(ex, "تعذر تجهيز إيصال صرف العمولة."), "إيصال صرف العمولة", MessageBoxButton.OK, MessageBoxImage.Warning); }
        }
        create.Click += (_, _) =>
        {
            var editor = new CommissionPayoutEditorWindow(service, representative.Code) { Owner = Window.GetWindow(this) };
            if (editor.ShowDialog() == true)
            {
                Refresh();
                refreshCommissionAccount();
            }
        };
        print.Click += (_, _) => PrintSelected();
        grid.MouseDoubleClick += (_, _) => PrintSelected();
        Refresh(); root.Children.Add(grid);
        return root;
    }

    private UIElement BuildCommissionAccount()
    {
        var root = new DockPanel { Margin = new Thickness(12) };
        var top = new StackPanel();
        var summary = new TextBlock { FontSize = 16, FontWeight = FontWeights.SemiBold, Margin = new Thickness(4, 4, 4, 10) };
        top.Children.Add(summary);
        top.Children.Add(new TextBlock { Text = "تُحتسب أرصدة الكشف من سجلات الاستحقاق والاعتماد وتوزيعات الصرف المحفوظة.", Foreground = Brushes.DimGray, Margin = new Thickness(4) });
        DockPanel.SetDock(top, Dock.Top); root.Children.Add(top);
        var grid = new DataGrid { IsReadOnly = true, CanUserAddRows = false, AutoGenerateColumns = false };
        AddColumns(grid, ("التاريخ", nameof(CommissionStatementRow.Date), 1.4), ("البيان", nameof(CommissionStatementRow.Type), 1.3),
            ("رقم الفاتورة / الإيصال", nameof(CommissionStatementRow.Reference), 2), ("عمولة مستحقة", nameof(CommissionStatementRow.Earned), 1),
            ("مبلغ مصروف", nameof(CommissionStatementRow.Paid), 1), ("الحالة", nameof(CommissionStatementRow.Status), 1.2));
        void Refresh()
        {
            summary.Text = $"العمولات المستحقة {service.GetAccruedCommission(representative.Code):N2} | المعتمدة {service.GetApprovedCommission(representative.Code):N2} | المصروفة {service.GetPaidCommission(representative.Code):N2} | المتبقي المعتمد للصرف {service.GetOutstandingCommission(representative.Code):N2}";
            var rows = service.CommissionAccruals.Where(x => x.RepresentativeCode.Equals(representative.Code, StringComparison.OrdinalIgnoreCase))
                .Select(x => new CommissionStatementRow(x.CollectedAt.ToString("yyyy/MM/dd HH:mm"), "استحقاق عمولة", x.InvoiceNumber + " / " + x.PaymentReceiptNumber, x.CommissionAmount, 0, GetAccrualStatusArabic(service.GetCommissionAccrualStatus(x))))
                .Concat(service.RepresentativePayouts.Where(x => x.RepresentativeCode.Equals(representative.Code, StringComparison.OrdinalIgnoreCase))
                    .Select(x => new CommissionStatementRow(x.Date.ToString("yyyy/MM/dd HH:mm"), "صرف عمولة", x.ReceiptNumber, 0, x.Amount, "مدفوعة")))
                .OrderBy(x => x.Date, StringComparer.Ordinal).ToList();
            grid.ItemsSource = rows;
        }
        refreshCommissionAccount = Refresh;
        Refresh();
        root.Children.Add(grid);
        return root;
    }

    private UIElement BuildPlaceholder(string message)
    {
        var panel = new StackPanel { Margin = new Thickness(20), VerticalAlignment = VerticalAlignment.Top };
        panel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, FontSize = 16, Foreground = Brushes.DimGray, Margin = new Thickness(4, 8, 4, 12) });
        panel.Children.Add(new TextBlock { Text = "0.00 ج.م — غير مطبق", FontSize = 20, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)) });
        return panel;
    }

    private List<CollectionRow> GetLinkedCollections()
    {
        var invoiceNumbers = representativeInvoices.Select(i => i.Number).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return service.Payments
            .SelectMany(payment => payment.Allocations
                .Where(allocation => invoiceNumbers.Contains(allocation.InvoiceNumber))
                .Select(allocation => new CollectionRow(payment.ReceiptNumber, payment.Date.ToString("yyyy/MM/dd HH:mm"), allocation.InvoiceNumber, allocation.Amount, payment.Method, payment.Note)))
            .OrderByDescending(row => row.Date, StringComparer.Ordinal)
            .ToList();
    }

    private static decimal GetInvoiceDiscount(Invoice invoice)
    {
        var beforeLineDiscounts = invoice.Lines.Sum(line => line.Quantity * line.UnitPrice);
        return Math.Max(0, beforeLineDiscounts - invoice.Total);
    }

    private static void AddSection(Panel panel, string title) => panel.Children.Add(new TextBlock { Text = title, FontSize = 17, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 10, 0, 5), Foreground = new SolidColorBrush(Color.FromRgb(30, 58, 95)) });
    private static string GetAccrualStatusArabic(CommissionAccrualStatus status) => status switch
    {
        CommissionAccrualStatus.Earned => "مستحقة",
        CommissionAccrualStatus.Approved => "معتمدة",
        CommissionAccrualStatus.PartiallyPaid => "مدفوعة جزئياً",
        CommissionAccrualStatus.Paid => "مدفوعة",
        _ => "غير محددة"
    };
    private static void AddInfo(Panel panel, string label, string value) => panel.Children.Add(new TextBlock { Text = $"{label}: {value}", Margin = new Thickness(4, 3, 4, 3), TextWrapping = TextWrapping.Wrap });

    private static void AddColumns(DataGrid grid, params (string Header, string Path, double Width)[] columns)
    {
        var numericPaths = new HashSet<string>
        {
            nameof(InvoiceRow.Subtotal), nameof(InvoiceRow.Discount), nameof(InvoiceRow.NetSales), nameof(InvoiceRow.Paid), nameof(InvoiceRow.Remaining),
            nameof(CollectionRow.Amount), nameof(InstallmentRow.Amount), nameof(InstallmentRow.Paid), nameof(InstallmentRow.Remaining), nameof(CommissionRow.Rate), nameof(CommissionRow.EligibleBase), nameof(CommissionRow.CollectedEligible), nameof(CommissionRow.Amount), nameof(CommissionRow.Paid), nameof(PayoutRow.Amount), nameof(CommissionStatementRow.Earned), nameof(CommissionStatementRow.Paid)
        };
        foreach (var column in columns)
            grid.Columns.Add(new DataGridTextColumn { Header = column.Header, Binding = new Binding(column.Path) { StringFormat = numericPaths.Contains(column.Path) ? "N2" : null }, Width = new DataGridLength(column.Width, DataGridLengthUnitType.Star) });
    }
}
