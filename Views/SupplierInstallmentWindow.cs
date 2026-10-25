using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using SmartErpDesktop.Models;
using SmartErpDesktop.Services;

namespace SmartErpDesktop.Views;

public sealed class SupplierInstallmentWindow : Window
{
    private readonly MockErpService service;
    private readonly Supplier supplier;
    private readonly string? purchaseNumber;
    private readonly DataGrid grid = new() { AutoGenerateColumns = true, IsReadOnly = true, CanUserAddRows = false, Margin = new Thickness(8) };
    private readonly TextBox amount = new() { Text = "0", Width = 110 };
    private readonly ComboBox method = new() { ItemsSource = new[] { "نقدي", "تحويل بنكي", "شيك" }, SelectedIndex = 0, Width = 130 };
    private readonly TextBox note = new() { Width = 160 };

    public SupplierInstallmentWindow(MockErpService service, Supplier supplier, string? purchaseNumber = null)
    {
        this.service = service;
        this.supplier = supplier;
        this.purchaseNumber = purchaseNumber;
        Title = purchaseNumber is null ? "أقساط المورد" : "أقساط الفاتورة " + purchaseNumber;
        Width = 1000;
        Height = 640;
        FlowDirection = FlowDirection.RightToLeft;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var root = new DockPanel { Margin = new Thickness(12) };
        var actions = new WrapPanel();
        var pay = new Button { Content = "تسجيل دفعة للقسط المحدد", Margin = new Thickness(4), Padding = new Thickness(10) };
        pay.Click += Pay;
        var preview = new Button { Content = "معاينة / طباعة الجدول", Margin = new Thickness(4), Padding = new Thickness(10) };
        preview.Click += (_, _) => PreviewSchedule();
        actions.Children.Add(pay);
        actions.Children.Add(new TextBlock { Text = "المبلغ", VerticalAlignment = VerticalAlignment.Center });
        actions.Children.Add(amount);
        actions.Children.Add(method);
        actions.Children.Add(note);
        actions.Children.Add(preview);
        DockPanel.SetDock(actions, Dock.Top);
        root.Children.Add(actions);
        root.Children.Add(grid);
        Content = root;
        Refresh();
    }

    private void Refresh()
    {
        grid.ItemsSource = service.SupplierInstallments
            .Where(i => i.SupplierName == supplier.Name && (purchaseNumber is null || i.PurchaseNumber == purchaseNumber))
            .OrderBy(i => i.PurchaseNumber).ThenBy(i => i.Sequence)
            .Select(i => new
            {
                i.Id,
                رقم_الشراء = i.PurchaseNumber,
                رقم_القسط  = i.Sequence,
                تاريخ_الاستحقاق = i.DueDate.ToString("yyyy/MM/dd"),
                قيمة_القسط = i.Amount,
                المدفوع   = i.PaidAmount,
                المتبقي   = i.Remaining,
                الحالة    = i.Status,
                تاريخ_السداد = i.PaidDate?.ToString("yyyy/MM/dd") ?? "—"
            }).ToList();
    }

    private void Pay(object? sender, RoutedEventArgs e)
    {
        var id   = grid.SelectedItem?.GetType().GetProperty("Id")?.GetValue(grid.SelectedItem)?.ToString();
        var item = service.SupplierInstallments.FirstOrDefault(x => x.Id == id);
        if (item is null) { MessageBox.Show("حدد قسطاً من الجدول."); return; }
        if (!decimal.TryParse(amount.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var value) &&
            !decimal.TryParse(amount.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
        { MessageBox.Show("أدخل مبلغاً صحيحاً."); return; }
        try
        {
            var receipt = service.RecordSupplierPayment(supplier.Name, value, method.SelectedItem?.ToString() ?? "نقدي", note.Text, item);
            MessageBox.Show($"تم تسجيل الإيصال {receipt.ReceiptNumber}");
            amount.Text = "0";
            note.Clear();
            Refresh();
        }
        catch (Exception ex) { MessageBox.Show(ArabicUi.ErrorMessage(ex, "تعذر تسجيل الدفعة. تحقق من البيانات وحاول مرة أخرى."), "تعذر تسجيل الدفعة", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private void PreviewSchedule()
    {
        Models.CompanySettings cs;
        try { cs = ((App)Application.Current).CompanySettings.GetSettings(); }
        catch { cs = new Models.CompanySettings(); }

        var installments = service.SupplierInstallments
            .Where(i => i.SupplierName == supplier.Name && (purchaseNumber is null || i.PurchaseNumber == purchaseNumber));

        var relatedPurchases = service.Purchases
            .Where(p => p.Supplier == supplier.Name && (purchaseNumber is null || p.Number == purchaseNumber)
                        && installments.Any(i => i.PurchaseNumber == p.Number));

        var doc = ErpPrintDocument.CreateSupplierInstallmentSchedule(supplier, relatedPurchases, installments, cs);
        ErpPrintDocument.CreatePreviewWindow(doc, "جدول أقساط المورد — " + supplier.Name,
            "جدول أقساط المورد", cs, this).ShowDialog();
    }
}
