using System.Windows;
using System.Windows.Controls;
using SmartErpDesktop.Models;
using SmartErpDesktop.Services;

namespace SmartErpDesktop.Views;

/// <summary>كشف حساب للعميل يجمع الفواتير والتحصيلات المسجلة.</summary>
public sealed class CustomerStatementWindow : Window
{
    public CustomerStatementWindow(MockErpService service, Customer customer)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(customer);

        Title = $"كشف حساب العميل - {customer.Name}";
        Width = 950;
        Height = 650;
        MinWidth = 720;
        MinHeight = 480;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FlowDirection = FlowDirection.RightToLeft;
        Background = System.Windows.Media.Brushes.White;

        var root = new DockPanel { Margin = new Thickness(16) };
        var header = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        header.Children.Add(new TextBlock
        {
            Text = "كشف حساب العميل",
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 8)
        });
        header.Children.Add(new TextBlock
        {
            Text = $"العميل: {customer.Name} | الكود: {customer.Code} | الهاتف: {customer.Phone}"
        });

        // Build statement rows using the shared ErpPrintDocument.StatementRow type
        var invoices = service.Invoices
            .Where(i => string.Equals(i.CustomerName, customer.Name, StringComparison.OrdinalIgnoreCase))
            .OrderBy(i => i.Date)
            .Select(i => new ErpPrintDocument.StatementRow(
                i.Date, "فاتورة", i.Number, i.Type, i.Total, i.Paid, i.Remaining, i.Status))
            .ToList();

        var payments = service.Payments
            .Where(p => string.Equals(p.CustomerName, customer.Name, StringComparison.OrdinalIgnoreCase))
            .Select(p => new ErpPrintDocument.StatementRow(
                p.Date, "تحصيل", p.ReceiptNumber, p.Method, 0m, p.Amount, p.BalanceAfter, p.Note))
            .ToList();

        var rows = invoices.Concat(payments).OrderBy(x => x.Date).ToList();
        var invoiceTotal = invoices.Sum(x => x.Amount);
        var collectedTotal = payments.Sum(x => x.Paid);

        header.Children.Add(new TextBlock
        {
            Text = $"إجمالي الفواتير: {invoiceTotal:N2} ج.م     |     إجمالي التحصيلات المسجلة: {collectedTotal:N2} ج.م     |     رصيد العميل الحالي: {customer.Balance:N2} ج.م",
            FontWeight = FontWeights.SemiBold,
            Foreground = System.Windows.Media.Brushes.DarkBlue,
            Margin = new Thickness(0, 8, 0, 0)
        });
        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);

        // DataGrid for on-screen display (keep as-is for usability)
        var gridRows = rows.Select(r => new
        {
            التاريخ = r.Date.ToString("yyyy/MM/dd"),
            النوع = r.Kind,
            الرقم = r.Number,
            البيان = r.Description,
            القيمة = r.Amount,
            المدفوع = r.Paid,
            الرصيد = r.Balance,
            الحالة = r.Status
        }).ToList();

        var grid = new DataGrid
        {
            ItemsSource = gridRows,
            AutoGenerateColumns = true,
            IsReadOnly = true,
            CanUserAddRows = false,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
            Margin = new Thickness(0, 4, 0, 10)
        };
        DockPanel.SetDock(grid, Dock.Top);
        root.Children.Add(grid);

        var footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left };

        var print = new Button { Content = "طباعة / معاينة الكشف", Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(0, 0, 8, 0) };
        print.Click += (_, _) =>
        {
            try
            {
                Models.CompanySettings cs;
                try { cs = ((App)Application.Current).CompanySettings.GetSettings(); }
                catch { cs = new Models.CompanySettings(); }
                var doc = ErpPrintDocument.CreateCustomerStatement(customer, rows, invoiceTotal, collectedTotal, cs);
                ErpPrintDocument.CreatePreviewWindow(doc, $"كشف حساب - {customer.Name}",
                    $"كشف حساب - {customer.Name}", cs, this).ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("تعذرت طباعة كشف الحساب: " + ex.Message, "خطأ في الطباعة",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };

        var close = new Button { Content = "إغلاق", Padding = new Thickness(14, 8, 14, 8) };
        close.Click += (_, _) => Close();
        footer.Children.Add(print);
        footer.Children.Add(close);
        DockPanel.SetDock(footer, Dock.Bottom);
        root.Children.Add(footer);
        Content = root;
    }
}
