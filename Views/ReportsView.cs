using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using SmartErpDesktop.Models;
using SmartErpDesktop.Services;
using System.IO;

namespace SmartErpDesktop.Views;

public sealed class ReportsView : UserControl
{
    private readonly MockErpService service;
    private readonly DatePicker fromDate = new() { SelectedDate = DateTime.Today.AddDays(-30), Width = 145, Margin = new Thickness(4) };
    private readonly DatePicker toDate = new() { SelectedDate = DateTime.Today, Width = 145, Margin = new Thickness(4) };
    private readonly TextBlock summary = new() { FontSize = 15, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(8) };
    private readonly DataGrid grid = new() { AutoGenerateColumns = true, IsReadOnly = true, Margin = new Thickness(4) };
    private readonly ComboBox reportType = new() { Width = 220, Margin = new Thickness(4) };

    public ReportsView(MockErpService data)
    {
        service = data;
        FlowDirection = FlowDirection.RightToLeft;
        var root = new DockPanel { LastChildFill = true, Margin = new Thickness(10) };
        var top = new StackPanel { Orientation = Orientation.Vertical };
        top.Children.Add(new TextBlock { Text = "التقارير والأرباح", FontSize = 24, FontWeight = FontWeights.Bold, Margin = new Thickness(4) });
        var filters = new WrapPanel { VerticalAlignment = VerticalAlignment.Center };
        filters.Children.Add(new TextBlock { Text = "من تاريخ:", VerticalAlignment = VerticalAlignment.Center }); filters.Children.Add(fromDate);
        filters.Children.Add(new TextBlock { Text = "إلى تاريخ:", VerticalAlignment = VerticalAlignment.Center }); filters.Children.Add(toDate);
        reportType.Items.Add("ملخص الأرباح"); reportType.Items.Add("المبيعات والفواتير"); reportType.Items.Add("المشتريات"); reportType.Items.Add("المصروفات"); reportType.Items.Add("أفضل الأصناف مبيعًا"); reportType.SelectedIndex = 0;
        filters.Children.Add(reportType);
        var refresh = new Button { Content = "عرض التقرير", Padding = new Thickness(14, 7, 14, 7), Margin = new Thickness(4) }; refresh.Click += (_, _) => RefreshReport(); filters.Children.Add(refresh);
        var export = new Button { Content = "تصدير CSV", Padding = new Thickness(14, 7, 14, 7), Margin = new Thickness(4) }; export.Click += (_, _) => ExportCsv(); filters.Children.Add(export);
        top.Children.Add(filters); top.Children.Add(summary);
        DockPanel.SetDock(top, Dock.Top); root.Children.Add(top); root.Children.Add(grid); Content = root;
        RefreshReport();
    }

    private (DateTime from, DateTime to) Range()
    {
        var from = (fromDate.SelectedDate ?? DateTime.Today.AddDays(-30)).Date;
        var to = (toDate.SelectedDate ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);
        if (from > to) throw new InvalidOperationException("تاريخ البداية لازم يكون قبل تاريخ النهاية.");
        return (from, to);
    }

    private void RefreshReport()
    {
        try
        {
            var (from, to) = Range();
            var invoices = service.Invoices.Where(x => x.Date >= from && x.Date <= to).ToList();
            var purchases = service.Purchases.Where(x => x.Date >= from && x.Date <= to).ToList();
            var expenses = service.Expenses.Where(x => x.Date >= from && x.Date <= to).ToList();
            var sales = invoices.Sum(x => x.Total);
            var cogs = invoices.SelectMany(x => x.Lines).Sum(line => line.Quantity * (service.Products.FirstOrDefault(p => p.Code == line.ProductCode)?.Cost ?? 0));
            var gross = sales - cogs;
            var expenseTotal = expenses.Sum(x => x.Amount);
            var net = gross - expenseTotal;
            summary.Text = $"الفترة: {from:yyyy/MM/dd} إلى {to:yyyy/MM/dd}\n" +
                $"إجمالي المبيعات: {sales:N2} ج.م    | تكلفة البضاعة التقديرية: {cogs:N2} ج.م\n" +
                $"مجمل الربح التقديري: {gross:N2} ج.م    | المصروفات: {expenseTotal:N2} ج.م\n" +
                $"صافي الربح التقديري: {net:N2} ج.م    | المشتريات: {purchases.Sum(x => x.Total):N2} ج.م\n" +
                $"عدد الفواتير: {invoices.Count}    | التحصيلات المسجلة: {service.Payments.Where(x => x.Date >= from && x.Date <= to).Sum(x => x.Amount):N2} ج.م";
            summary.Foreground = net >= 0 ? Brushes.SeaGreen : Brushes.Firebrick;
            object rows = reportType.SelectedItem?.ToString() switch
            {
                "المبيعات والفواتير" => invoices.OrderByDescending(x => x.Date).Select(x => new { رقم_الفاتورة = x.Number, التاريخ = x.Date.ToString("yyyy/MM/dd"), العميل = x.CustomerName, الإجمالي = x.Total, المدفوع = x.Paid, المتبقي = x.Remaining, التكلفة_التقديرية = x.Lines.Sum(l => l.Quantity * (service.Products.FirstOrDefault(p => p.Code == l.ProductCode)?.Cost ?? 0)), الربح_التقديري = x.Total - x.Lines.Sum(l => l.Quantity * (service.Products.FirstOrDefault(p => p.Code == l.ProductCode)?.Cost ?? 0)) }).ToList(),
                "المشتريات" => purchases.OrderByDescending(x => x.Date).Select(x => new { رقم_الشراء = x.Number, التاريخ = x.Date.ToString("yyyy/MM/dd"), المورد = x.Supplier, الإجمالي = x.Total, المدفوع = x.Paid, المتبقي = x.Remaining, الحالة = x.Status }).ToList(),
                "المصروفات" => expenses.OrderByDescending(x => x.Date).Select(x => new { الرقم = x.Number, التاريخ = x.Date.ToString("yyyy/MM/dd"), التصنيف = x.Category, البيان = x.Description, المبلغ = x.Amount, طريقة_الدفع = x.PaymentMethod }).ToList(),
                "أفضل الأصناف مبيعًا" => invoices.SelectMany(i => i.Lines).GroupBy(l => new { l.ProductCode, l.ProductName }).Select(g => new { الكود = g.Key.ProductCode, الصنف = g.Key.ProductName, الكمية_المباعة = g.Sum(l => l.Quantity), قيمة_المبيعات = g.Sum(l => l.Total) }).OrderByDescending(x => x.الكمية_المباعة).ToList(),
                _ => new[] { new { البيان = "إجمالي المبيعات", القيمة = sales }, new { البيان = "تكلفة البضاعة التقديرية", القيمة = cogs }, new { البيان = "مجمل الربح التقديري", القيمة = gross }, new { البيان = "المصروفات", القيمة = expenseTotal }, new { البيان = "صافي الربح التقديري", القيمة = net } }.ToList()
            };
            grid.ItemsSource = (System.Collections.IEnumerable)rows;
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "التقارير", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private void ExportCsv()
    {
        if (grid.ItemsSource is not System.Collections.IEnumerable rows) return;
        var dialog = new SaveFileDialog { Filter = "CSV UTF-8 (*.csv)|*.csv", FileName = $"ERP_Report_{DateTime.Today:yyyyMMdd}.csv" };
        if (dialog.ShowDialog() != true) return;
        var list = rows.Cast<object>().ToList();
        if (list.Count == 0) { MessageBox.Show("لا توجد بيانات للتصدير."); return; }
        var props = list[0].GetType().GetProperties();
        var sb = new StringBuilder("\uFEFF");
        sb.AppendLine(string.Join(",", props.Select(p => Escape(p.Name))));
        foreach (var row in list) sb.AppendLine(string.Join(",", props.Select(p => Escape(Convert.ToString(p.GetValue(row), CultureInfo.CurrentCulture) ?? ""))));
        File.WriteAllText(dialog.FileName, sb.ToString(), new UTF8Encoding(true));
        MessageBox.Show("تم تصدير التقرير بنجاح.", "تصدير", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    private static string Escape(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
}
