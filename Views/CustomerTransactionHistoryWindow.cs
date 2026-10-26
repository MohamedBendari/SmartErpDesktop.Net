using System.Windows;
using System.Windows.Controls;
using SmartErpDesktop.Models;
using SmartErpDesktop.Services;

namespace SmartErpDesktop.Views;

/// <summary>Read-only transaction history for one customer/company, with invoice-scoped installment navigation.</summary>
public sealed class CustomerTransactionHistoryWindow : Window
{
    private sealed record HistoryRow(string Date, string Type, string Reference, string Description,
        decimal InvoiceTotal, decimal Paid, decimal Remaining, string Status, Invoice? Invoice, Payment? Payment);
    private sealed record InstallmentRow(int Sequence, string DueDate, decimal Amount, decimal Paid,
        decimal Remaining, string Status, string Receipts);

    private readonly MockErpService service;
    private readonly Customer customer;
    private readonly StackPanel historyPanel = new();
    private readonly StackPanel schedulePanel = new();
    private readonly DataGrid historyGrid = new() { IsReadOnly = true, CanUserAddRows = false, AutoGenerateColumns = false, SelectionMode = DataGridSelectionMode.Single };
    private readonly DataGrid scheduleGrid = new() { IsReadOnly = true, CanUserAddRows = false, AutoGenerateColumns = false };
    private readonly TextBlock scheduleInfo = new() { Margin = new Thickness(0, 3, 0, 8) };
    private readonly DataGrid invoicePaymentsGrid = new() { IsReadOnly = true, CanUserAddRows = false, AutoGenerateColumns = true, MaxHeight = 140 };
    private readonly TextBox search = new() { Width = 190, Margin = new Thickness(3), ToolTip = "بحث برقم الفاتورة أو الإيصال أو النوع" };
    private readonly ComboBox typeFilter = new() { Width = 130, Margin = new Thickness(3) };
    private readonly DatePicker from = new() { Width = 135, Margin = new Thickness(3) };
    private readonly DatePicker to = new() { Width = 135, Margin = new Thickness(3) };
    private readonly TextBlock summary = new() { Margin = new Thickness(5), FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap };
    private List<HistoryRow> allRows = new();

    public CustomerTransactionHistoryWindow(MockErpService service, Customer customer)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
        this.customer = customer ?? throw new ArgumentNullException(nameof(customer));
        Title = $"سجل المعاملات - {customer.Name}";
        Width = 1120; Height = 720; MinWidth = 850; MinHeight = 550;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FlowDirection = FlowDirection.RightToLeft;
        Background = System.Windows.Media.Brushes.White;

        var root = new Grid { Margin = new Thickness(12) };
        historyPanel.Children.Add(new TextBlock { Text = "سجل معاملات العميل / الشركة", FontSize = 22, FontWeight = FontWeights.Bold });
        historyPanel.Children.Add(new TextBlock { Text = $"{customer.Name} | الكود: {customer.Code} | الرصيد الحالي: {customer.Balance:N2} ج.م", Margin = new Thickness(0, 2, 0, 8) });
        var filters = new WrapPanel();
        typeFilter.Items.Add("كل الحركات"); typeFilter.Items.Add("الفواتير"); typeFilter.Items.Add("التحصيلات"); typeFilter.SelectedIndex = 0;
        filters.Children.Add(typeFilter); filters.Children.Add(from); filters.Children.Add(to); filters.Children.Add(search);
        var filterButton = new Button { Content = "تطبيق التصفية", Margin = new Thickness(3) };
        var resetButton = new Button { Content = "إعادة التصفية", Margin = new Thickness(3) };
        filterButton.Click += (_, _) => RefreshRows();
        resetButton.Click += (_, _) => { search.Clear(); from.SelectedDate = null; to.SelectedDate = null; typeFilter.SelectedIndex = 0; RefreshRows(); };
        filters.Children.Add(filterButton); filters.Children.Add(resetButton);
        historyPanel.Children.Add(filters);
        historyPanel.Children.Add(summary);
        AddHistoryColumns();
        historyGrid.MouseDoubleClick += (_, _) => OpenSelectedDetails();
        historyPanel.Children.Add(historyGrid);

        var actions = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
        var details = new Button { Content = "عرض التفاصيل", Margin = new Thickness(3) };
        var installments = new Button { Content = "عرض جدول الأقساط", Margin = new Thickness(3) };
        var statement = new Button { Content = "عرض كشف الحساب", Margin = new Thickness(3) };
        var close = new Button { Content = "إغلاق", Margin = new Thickness(3) };
        details.Click += (_, _) => OpenSelectedDetails();
        installments.Click += (_, _) => ShowSelectedSchedule();
        statement.Click += (_, _) => new CustomerStatementWindow(service, customer) { Owner = this }.ShowDialog();
        close.Click += (_, _) => Close();
        actions.Children.Add(details); actions.Children.Add(installments); actions.Children.Add(statement); actions.Children.Add(close);
        historyPanel.Children.Add(actions);

        schedulePanel.Visibility = Visibility.Collapsed;
        schedulePanel.Children.Add(new TextBlock { Text = "جدول أقساط الفاتورة", FontSize = 22, FontWeight = FontWeights.Bold });
        schedulePanel.Children.Add(scheduleInfo);
        AddScheduleColumns();
        schedulePanel.Children.Add(scheduleGrid);
        schedulePanel.Children.Add(new TextBlock { Text = "إيصالات التحصيل المرتبطة بالفاتورة", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 3) });
        schedulePanel.Children.Add(invoicePaymentsGrid);
        var back = new Button { Content = "العودة إلى سجل المعاملات", Margin = new Thickness(3, 8, 3, 3), HorizontalAlignment = HorizontalAlignment.Left };
        back.Click += (_, _) => ReturnToHistory();
        schedulePanel.Children.Add(back);

        root.Children.Add(historyPanel); root.Children.Add(schedulePanel); Content = root;
        LoadRows(); RefreshRows();

        void AddScheduleColumns()
        {
            scheduleGrid.Columns.Add(Column("رقم القسط", nameof(InstallmentRow.Sequence), 0.7));
            scheduleGrid.Columns.Add(Column("تاريخ الاستحقاق", nameof(InstallmentRow.DueDate), 1.2));
            scheduleGrid.Columns.Add(Column("القيمة", nameof(InstallmentRow.Amount), 1));
            scheduleGrid.Columns.Add(Column("المدفوع", nameof(InstallmentRow.Paid), 1));
            scheduleGrid.Columns.Add(Column("المتبقي", nameof(InstallmentRow.Remaining), 1));
            scheduleGrid.Columns.Add(Column("الحالة", nameof(InstallmentRow.Status), 1));
            scheduleGrid.Columns.Add(Column("إيصالات القسط", nameof(InstallmentRow.Receipts), 2));
        }
    }

    private void LoadRows()
    {
        var invoices = service.GetCustomerInvoices(customer);
        var payments = service.GetCustomerPayments(customer);
        allRows = invoices.Select(invoice => new HistoryRow(invoice.Date.ToString("yyyy/MM/dd HH:mm"), "فاتورة بيع", invoice.Number,
                $"{invoice.Type} | خصم: {invoice.DiscountPercent:N2}% + خصم البنود: {invoice.Lines.Sum(line => line.Discount):N2}", invoice.Total, invoice.Paid, invoice.Remaining, invoice.Status, invoice, null))
            .Concat(payments.Select(payment =>
            {
                var linked = payment.Allocations.Where(a => invoices.Any(i => i.Number.Equals(a.InvoiceNumber, StringComparison.OrdinalIgnoreCase))).ToList();
                var reference = linked.Count > 0 ? string.Join(", ", linked.Select(a => a.InvoiceNumber).Distinct(StringComparer.OrdinalIgnoreCase)) : "—";
                var desc = payment.InstallmentId.Length > 0
                    ? $"إيصال: {payment.ReceiptNumber} | القسط: {service.Installments.FirstOrDefault(i => i.Id == payment.InstallmentId)?.Sequence.ToString() ?? "—"} | {payment.Method}"
                    : $"إيصال: {payment.ReceiptNumber} | {payment.Method} | {payment.Note}";
                return new HistoryRow(payment.Date.ToString("yyyy/MM/dd HH:mm"), "تحصيل", reference, desc, 0, payment.Amount, 0, "مسجل", null, payment);
            }))
            .OrderByDescending(row => DateTime.TryParse(row.Date, out var date) ? date : DateTime.MinValue)
            .ThenBy(row => row.Reference, StringComparer.Ordinal).ToList();
    }

    private void RefreshRows()
    {
        var query = search.Text.Trim();
        var rows = allRows.Where(row => typeFilter.SelectedIndex switch { 1 => row.Invoice is not null, 2 => row.Payment is not null, _ => true })
            .Where(row => from.SelectedDate is null || (DateTime.TryParse(row.Date, out var date) && date.Date >= from.SelectedDate.Value.Date))
            .Where(row => to.SelectedDate is null || (DateTime.TryParse(row.Date, out var date) && date.Date <= to.SelectedDate.Value.Date))
            .Where(row => query.Length == 0 || row.Reference.Contains(query, StringComparison.OrdinalIgnoreCase) || row.Type.Contains(query, StringComparison.OrdinalIgnoreCase) || row.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
        historyGrid.ItemsSource = rows;
        summary.Text = rows.Count == 0 ? "لا توجد معاملات مطابقة." :
            $"عدد الحركات: {rows.Count} | إجمالي الفواتير: {rows.Where(x => x.Invoice is not null).Sum(x => x.InvoiceTotal):N2} ج.م | إجمالي التحصيلات: {rows.Where(x => x.Payment is not null).Sum(x => x.Paid):N2} ج.م | الرصيد الحالي: {customer.Balance:N2} ج.م";
    }

    private void AddHistoryColumns()
    {
        historyGrid.Columns.Add(Column("التاريخ", nameof(HistoryRow.Date), 1.3));
        historyGrid.Columns.Add(Column("النوع", nameof(HistoryRow.Type), 0.9));
        historyGrid.Columns.Add(Column("رقم الفاتورة / الإيصال", nameof(HistoryRow.Reference), 1.3));
        historyGrid.Columns.Add(Column("البيان", nameof(HistoryRow.Description), 2));
        historyGrid.Columns.Add(Column("إجمالي الفاتورة", nameof(HistoryRow.InvoiceTotal), 1));
        historyGrid.Columns.Add(Column("المبلغ المدفوع / المحصل", nameof(HistoryRow.Paid), 1.2));
        historyGrid.Columns.Add(Column("المتبقي", nameof(HistoryRow.Remaining), 0.9));
        historyGrid.Columns.Add(Column("الحالة", nameof(HistoryRow.Status), 0.9));
    }

    private static DataGridTextColumn Column(string title, string path, double width) =>
        new() { Header = title, Binding = new System.Windows.Data.Binding(path), Width = new DataGridLength(width, DataGridLengthUnitType.Star) };

    private void OpenSelectedDetails()
    {
        if (historyGrid.SelectedItem is not HistoryRow row) return;
        if (row.Invoice is { } invoice)
        {
            var lines = string.Join(Environment.NewLine, invoice.Lines.Select(line => $"{line.ProductName} | الكمية: {line.Quantity:N2} | سعر الوحدة: {line.UnitPrice:N2} | الإجمالي: {line.Total:N2}"));
            MessageBox.Show($"الفاتورة: {invoice.Number}\nالتاريخ: {invoice.Date:yyyy/MM/dd HH:mm}\nالنوع: {invoice.Type}\nالإجمالي: {invoice.Total:N2}\nالخصم: {invoice.DiscountPercent:N2}%\nالمدفوع: {invoice.Paid:N2}\nالمتبقي: {invoice.Remaining:N2}\n\n{lines}", "تفاصيل الفاتورة", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else if (row.Payment is { } payment)
            MessageBox.Show($"إيصال التحصيل: {payment.ReceiptNumber}\nالتاريخ: {payment.Date:yyyy/MM/dd HH:mm}\nالمبلغ: {payment.Amount:N2}\nطريقة الدفع: {payment.Method}\nالفواتير المرتبطة: {string.Join(", ", payment.Allocations.Select(x => x.InvoiceNumber))}\nملاحظات: {payment.Note}", "تفاصيل التحصيل", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ShowSelectedSchedule()
    {
        try
        {
            if (historyGrid.SelectedItem is not HistoryRow { Invoice: { } invoice })
            { MessageBox.Show("حدد فاتورة بيع بالتقسيط أولاً.", "جدول الأقساط", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            var installments = service.GetInstallments(invoice.Number);
            if (installments.Count == 0)
            { MessageBox.Show("لا يوجد جدول أقساط مسجل لهذه الفاتورة.", "جدول الأقساط", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            var payments = service.GetPaymentsForInvoice(invoice.Number);
            var initialPayment = invoice.InitialPayment ?? payments
                .Where(payment => payment.SourceType == PaymentSourceType.InvoiceInitialPayment)
                .Select(payment => (decimal?)payment.Amount).FirstOrDefault() ?? 0m;
            scheduleInfo.Text = $"الفاتورة: {invoice.Number} | التاريخ: {invoice.Date:yyyy/MM/dd} | قيمة الفاتورة: {invoice.Total:N2} | المقدم: {initialPayment:N2} | الممول: {Math.Max(0, invoice.Total - initialPayment):N2} | المدفوع: {invoice.Paid:N2} | المتبقي: {invoice.Remaining:N2} | الحالة: {invoice.Status}";
            scheduleGrid.ItemsSource = installments.Select(item => new InstallmentRow(item.Sequence, item.DueDate.ToString("yyyy/MM/dd"), item.Amount, item.PaidAmount,
                item.Remaining, item.Status,
                string.Join(", ", payments.Where(p => p.InstallmentId == item.Id).Select(p => p.ReceiptNumber)))).ToList();
            invoicePaymentsGrid.ItemsSource = payments.Select(payment => new
            {
                الإيصال = payment.ReceiptNumber,
                التاريخ = payment.Date.ToString("yyyy/MM/dd HH:mm"),
                المبلغ = payment.Amount,
                طريقة_الدفع = payment.Method,
                القسط_المحدد = payment.InstallmentId.Length == 0 ? "غير محدد في السجل القديم" : installments.FirstOrDefault(i => i.Id == payment.InstallmentId)?.Sequence.ToString() ?? "—"
            }).ToList();
            historyPanel.Visibility = Visibility.Collapsed;
            schedulePanel.Visibility = Visibility.Visible;
        }
        catch (Exception exception)
        {
            LocalErrorLog.Write("CustomerTransactionHistory.ShowSelectedSchedule", exception);
            MessageBox.Show("تعذر عرض جدول الأقساط. لم يتم إجراء أي تغيير على البيانات. راجع الدعم الفني إذا تكرر الخطأ.",
                "جدول الأقساط", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ReturnToHistory()
    {
        schedulePanel.Visibility = Visibility.Collapsed;
        historyPanel.Visibility = Visibility.Visible;
    }
}
