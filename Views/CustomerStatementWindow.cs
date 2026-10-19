using System.Globalization;
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
        header.Children.Add(new TextBlock { Text = $"العميل: {customer.Name} | الكود: {customer.Code} | الهاتف: {customer.Phone}" });

        var invoices = service.Invoices
            .Where(i => string.Equals(i.CustomerName, customer.Name, StringComparison.OrdinalIgnoreCase))
            .OrderBy(i => i.Date)
            .Select(i => new StatementRow(i.Date, "فاتورة", i.Number,
                i.Type, i.Total, i.Paid, i.Remaining, i.Status))
            .ToList();
        var payments = service.Payments
            .Where(p => string.Equals(p.CustomerName, customer.Name, StringComparison.OrdinalIgnoreCase))
            .Select(p => new StatementRow(p.Date, "تحصيل", p.ReceiptNumber,
                p.Method, 0m, p.Amount, p.BalanceAfter, p.Note))
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

        var grid = new DataGrid
        {
            ItemsSource = rows,
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
        var print = new Button { Content = "طباعة الكشف", Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(0, 0, 8, 0) };
        print.Click += (_, _) => PrintStatement(customer, rows, invoiceTotal, collectedTotal);
        var close = new Button { Content = "إغلاق", Padding = new Thickness(14, 8, 14, 8) };
        close.Click += (_, _) => Close();
        footer.Children.Add(print);
        footer.Children.Add(close);
        DockPanel.SetDock(footer, Dock.Bottom);
        root.Children.Add(footer);
        Content = root;
    }

    private static void PrintStatement(Customer customer, List<StatementRow> rows, decimal invoices, decimal collected)
    {
        var dialog = new System.Windows.Controls.PrintDialog();
        if (dialog.ShowDialog() != true) return;

        var document = new System.Windows.Documents.FlowDocument
        {
            FlowDirection = FlowDirection.RightToLeft,
            PagePadding = new Thickness(35),
            ColumnWidth = 900
        };
        document.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run("كشف حساب العميل"))
        {
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center
        });
        document.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"العميل: {customer.Name} | الكود: {customer.Code} | الهاتف: {customer.Phone}")));
        document.Blocks.Add(new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run($"إجمالي الفواتير: {invoices:N2} ج.م | التحصيلات: {collected:N2} ج.م | الرصيد الحالي: {customer.Balance:N2} ج.م")));

        var table = new System.Windows.Documents.Table { CellSpacing = 0 };
        foreach (var width in new[] { 90d, 65d, 110d, 100d, 85d, 85d, 85d, 150d })
            table.Columns.Add(new System.Windows.Documents.TableColumn { Width = new GridLength(width) });
        var group = new System.Windows.Documents.TableRowGroup();
        table.RowGroups.Add(group);
        AddPrintRow(group, new[] { "التاريخ", "النوع", "الرقم", "البيان", "القيمة", "المدفوع", "الرصيد", "الحالة/ملاحظات" }, true);
        foreach (var row in rows)
            AddPrintRow(group, new[] { row.Date.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture), row.Kind, row.Number, row.Description,
                row.Amount.ToString("N2"), row.Paid.ToString("N2"), row.Balance.ToString("N2"), row.Status }, false);
        document.Blocks.Add(table);
        document.PageWidth = dialog.PrintableAreaWidth;
        document.PageHeight = dialog.PrintableAreaHeight;
        document.ColumnWidth = dialog.PrintableAreaWidth;
        var paginator = ((System.Windows.Documents.IDocumentPaginatorSource)document).DocumentPaginator;
        paginator.PageSize = new System.Windows.Size(dialog.PrintableAreaWidth, dialog.PrintableAreaHeight);
        dialog.PrintDocument(paginator, $"كشف حساب - {customer.Name}");
    }

    private static void AddPrintRow(System.Windows.Documents.TableRowGroup group, string[] values, bool header)
    {
        var row = new System.Windows.Documents.TableRow();
        foreach (var value in values)
        {
            var paragraph = new System.Windows.Documents.Paragraph(new System.Windows.Documents.Run(value)) { Margin = new Thickness(3) };
            if (header) paragraph.FontWeight = FontWeights.Bold;
            row.Cells.Add(new System.Windows.Documents.TableCell(paragraph)
            {
                BorderBrush = System.Windows.Media.Brushes.Gray,
                BorderThickness = new Thickness(0.5),
                Padding = new Thickness(3)
            });
        }
        group.Rows.Add(row);
    }

    private sealed record StatementRow(DateTime Date, string Kind, string Number, string Description,
        decimal Amount, decimal Paid, decimal Balance, string Status);
}
