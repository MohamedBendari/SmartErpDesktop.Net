using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using SmartErpDesktop.Models;
using SmartErpDesktop.Services;

namespace SmartErpDesktop.Views;

public sealed class SupplierStatementWindow : Window
{
    private readonly MockErpService service;
    private readonly ComboBox supplier = new() { MinWidth = 250, DisplayMemberPath = "Name" };
    private readonly DataGrid grid = new() { AutoGenerateColumns = true, IsReadOnly = true, CanUserAddRows = false, Height = 300, Margin = new Thickness(4) };
    private readonly TextBox amount = new() { Width = 120, Text = "0" };
    private readonly ComboBox method = new() { Width = 130, ItemsSource = new[] { "نقدي", "تحويل بنكي", "شيك", "بطاقة" }, SelectedIndex = 0 };
    private readonly TextBox note = new() { Width = 220 };
    private readonly TextBlock balance = new() { FontSize = 17, FontWeight = FontWeights.Bold, Margin = new Thickness(6) };

    public SupplierStatementWindow(MockErpService service)
    {
        this.service = service;
        Title = "كشف حساب المورد والسداد";
        Width = 850;
        Height = 600;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FlowDirection = FlowDirection.RightToLeft;

        supplier.ItemsSource = service.Suppliers;
        if (service.Suppliers.Count > 0) supplier.SelectedIndex = 0;
        supplier.SelectionChanged += (_, _) => RefreshStatement();

        var save = new Button { Content = "تسجيل دفعة للمورد", Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(4) };
        save.Click += SavePayment;

        var root = new StackPanel { Margin = new Thickness(12) };
        root.Children.Add(new TextBlock { Text = "كشف حساب المورد", FontSize = 22, FontWeight = FontWeights.Bold, Margin = new Thickness(4) });

        var printStatement = new Button { Content = "معاينة / طباعة كشف المورد", Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(4) };
        printStatement.Click += (_, _) => PreviewStatement();

        var installments = new Button { Content = "جدول أقساط المورد", Padding = new Thickness(12, 7, 12, 7), Margin = new Thickness(4) };
        installments.Click += (_, _) =>
        {
            if (supplier.SelectedItem is Supplier selected)
                new SupplierInstallmentWindow(service, selected) { Owner = this }.ShowDialog();
        };

        root.Children.Add(Row("المورد", supplier, printStatement, installments));
        root.Children.Add(balance);
        root.Children.Add(grid);
        root.Children.Add(Row("مبلغ الدفعة", amount, "طريقة الدفع", method, "ملاحظات", note, save));
        Content = root;
        RefreshStatement();
    }

    private static UIElement Row(params object[] items)
    {
        var p = new WrapPanel { Margin = new Thickness(4) };
        foreach (var x in items)
        {
            if (x is string label)
                p.Children.Add(new TextBlock { Text = label, Margin = new Thickness(6), VerticalAlignment = VerticalAlignment.Center });
            else if (x is UIElement el)
                p.Children.Add(el);
        }
        return p;
    }

    private void RefreshStatement()
    {
        if (supplier.SelectedItem is not Supplier s)
        {
            grid.ItemsSource = null;
            balance.Text = "لا يوجد موردون مسجلون";
            return;
        }
        balance.Text = $"الرصيد المستحق للمورد: {s.Balance:N2}";
        var rows = new List<object>();
        rows.AddRange(service.Purchases
            .Where(p => p.Supplier == s.Name)
            .Select(p => (object)new { التاريخ = p.Date.ToString("yyyy/MM/dd"), النوع = "فاتورة شراء", الرقم = p.Number, مدين = p.Total, مدفوع = p.Paid, الرصيد = p.Remaining, التفاصيل = p.Status }));
        rows.AddRange(service.SupplierPayments
            .Where(p => p.SupplierName == s.Name)
            .Select(p => (object)new { التاريخ = p.Date.ToString("yyyy/MM/dd"), النوع = "دفعة مورد", الرقم = p.ReceiptNumber, مدين = 0m, مدفوع = p.Amount, الرصيد = p.BalanceAfter, التفاصيل = p.Method + " " + p.Note }));
        grid.ItemsSource = rows.OrderBy(x => x.GetType().GetProperty("التاريخ")?.GetValue(x)?.ToString()).ToList();
    }

    private void SavePayment(object? sender, RoutedEventArgs e)
    {
        if (supplier.SelectedItem is not Supplier s) { MessageBox.Show("اختار المورد."); return; }
        if (!decimal.TryParse(amount.Text, System.Globalization.NumberStyles.Number, CultureInfo.CurrentCulture, out var value) &&
            !decimal.TryParse(amount.Text, System.Globalization.NumberStyles.Number, CultureInfo.InvariantCulture, out value))
        { MessageBox.Show("أدخل مبلغًا صحيحًا."); return; }
        try
        {
            var receipt = service.RecordSupplierPayment(s.Name, value, method.SelectedItem?.ToString() ?? "نقدي", note.Text);
            MessageBox.Show($"تم تسجيل الإيصال {receipt.ReceiptNumber}", "نجاح");
            amount.Text = "0";
            note.Clear();
            RefreshStatement();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "تعذر تسجيل الدفعة", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private void PreviewStatement()
    {
        if (supplier.SelectedItem is not Supplier s) { MessageBox.Show("اختار المورد أولاً."); return; }

        Models.CompanySettings cs;
        try { cs = ((App)Application.Current).CompanySettings.GetSettings(); }
        catch { cs = new Models.CompanySettings(); }

        var purchases = service.Purchases.Where(p => p.Supplier == s.Name);
        var payments  = service.SupplierPayments.Where(p => p.SupplierName == s.Name);
        var doc = ErpPrintDocument.CreateSupplierStatement(s, purchases, payments, cs);
        ErpPrintDocument.CreatePreviewWindow(doc, $"كشف حساب المورد — {s.Name}",
            "كشف حساب المورد " + s.Code, cs, this).ShowDialog();
    }
}
