using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using SmartErpDesktop.Models;
using SmartErpDesktop.Services;

namespace SmartErpDesktop.Views;

public sealed class PurchaseEditorWindow : Window
{
    private readonly MockErpService service;
    private readonly ComboBox supplier = new() { MinWidth = 240, DisplayMemberPath = "Name" };
    private readonly ComboBox product = new() { MinWidth = 260, DisplayMemberPath = "Name" };
    private readonly TextBox quantity = new() { Text = "1", Width = 85 };
    private readonly TextBox cost = new() { Width = 100 };
    private readonly TextBox discount = new() { Text = "0", Width = 90 };
    private readonly TextBox paid = new() { Text = "0", Width = 100 };
    private readonly TextBox invoiceDiscount = new() { Text = "0", Width = 100 };
    private readonly TextBox installmentMonths = new() { Text = "0", Width = 80 };
    private readonly DatePicker firstDueDate = new() { SelectedDate = DateTime.Today.AddMonths(1), Width = 150 };
    private readonly DatePicker purchaseDate = new() { SelectedDate = DateTime.Today, Width = 150 };
    private readonly CheckBox receive = new() { Content = "استلام البضاعة وتحديث المخزون الآن", IsChecked = true, Margin = new Thickness(4) };
    private readonly DataGrid grid = new() { AutoGenerateColumns = true, CanUserAddRows = false, Height = 230, Margin = new Thickness(4) };
    private readonly List<PurchaseLine> lines = new();
    private readonly TextBlock totalText = new() { FontSize = 16, FontWeight = FontWeights.Bold, Margin = new Thickness(6) };

    public PurchaseEditorWindow(MockErpService service)
    {
        this.service = service;
        Title = "فاتورة شراء جديدة"; Width = 850; Height = 650; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FlowDirection = FlowDirection.RightToLeft; Background = System.Windows.Media.Brushes.White;
        supplier.ItemsSource = service.Suppliers.Where(x => x.IsActive).ToList();
        if (supplier.Items.Count > 0) supplier.SelectedIndex = 0;
        product.ItemsSource = service.Products;
        product.SelectionChanged += (_, _) => { if (product.SelectedItem is Product p) cost.Text = p.Cost.ToString("0.##", CultureInfo.CurrentCulture); };
        var add = new Button { Content = "إضافة الصنف", Padding = new Thickness(12, 6, 12, 6), Margin = new Thickness(4) };
        add.Click += (_, _) => AddLine();
        var save = new Button { Content = "حفظ الفاتورة", Padding = new Thickness(16, 8, 16, 8), Margin = new Thickness(4) };
        save.Click += SavePurchase;
        var root = new StackPanel { Margin = new Thickness(14) };
        root.Children.Add(new TextBlock { Text = "فاتورة شراء جديدة", FontSize = 22, FontWeight = FontWeights.Bold, Margin = new Thickness(4) });
        root.Children.Add(Row("المورد", supplier, "تاريخ الشراء", purchaseDate));
        root.Children.Add(Row("الصنف", product));
        root.Children.Add(Row("الكمية", quantity, "تكلفة الوحدة", cost, "خصم السطر", discount, add));
        root.Children.Add(grid);
        root.Children.Add(Row("خصم الفاتورة", invoiceDiscount, "المدفوع الآن", paid, "عدد الأقساط (0 = بدون تقسيط)", installmentMonths, "أول استحقاق", firstDueDate)); root.Children.Add(receive); root.Children.Add(totalText); root.Children.Add(save);
        Content = new ScrollViewer { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        RefreshLines();
    }

    private static UIElement Row(params object[] items)
    {
        var panel = new WrapPanel { Margin = new Thickness(4), VerticalAlignment = VerticalAlignment.Center };
        foreach (var item in items)
        {
            if (item is string label) panel.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 4, 4, 4) });
            else if (item is UIElement element) panel.Children.Add(element);
        }
        return panel;
    }

    private void AddLine()
    {
        if (product.SelectedItem is not Product p) { MessageBox.Show("اختار الصنف أولاً."); return; }
        if (!Parse(quantity.Text, out var qty) || !Parse(cost.Text, out var unitCost) || !Parse(discount.Text, out var disc) || qty <= 0 || unitCost < 0 || disc < 0 || disc > qty * unitCost)
        { MessageBox.Show("راجع الكمية والتكلفة والخصم."); return; }
        lines.Add(new PurchaseLine { ProductCode = p.Code, ProductName = p.Name, Quantity = qty, UnitCost = unitCost, Discount = disc });
        RefreshLines();
    }

    private void RefreshLines()
    {
        grid.ItemsSource = null; grid.ItemsSource = lines.Select(x => new { كود_الصنف = x.ProductCode, الصنف = x.ProductName, الكمية = x.Quantity, تكلفة_الوحدة = x.UnitCost, الخصم = x.Discount, الإجمالي = x.Total }).ToList();
        decimal.TryParse(invoiceDiscount.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var off);
        totalText.Text = $"إجمالي الفاتورة: {Math.Max(0, lines.Sum(x => x.Total) - off):N2}";
    }

    private void SavePurchase(object? sender, RoutedEventArgs e)
    {
        if (supplier.SelectedItem is not Supplier s) { MessageBox.Show("اختار المورد."); return; }
        if (!Parse(paid.Text, out var paidAmount)) { MessageBox.Show("قيمة المدفوع غير صحيحة."); return; }
        try
        {
            if (!int.TryParse(installmentMonths.Text, out var months) || months < 0 || months > 120 || (months > 0 && firstDueDate.SelectedDate is null)) { MessageBox.Show("مدة التقسيط تكون صفراً أو من شهر إلى 120 شهراً، مع تحديد أول استحقاق."); return; }
            if (!Parse(invoiceDiscount.Text, out var off)) { MessageBox.Show("خصم الفاتورة غير صحيح."); return; }
            var purchase = service.CreatePurchase(s.Name, lines, paidAmount, receive.IsChecked == true, months, firstDueDate.SelectedDate ?? DateTime.Today.AddMonths(1), purchaseDate.SelectedDate ?? DateTime.Today, off);
            MessageBox.Show($"تم حفظ الفاتورة {purchase.Number}\nالإجمالي: {purchase.Total:N2}\nالمدفوع: {purchase.Paid:N2}\nالمتبقي: {purchase.Remaining:N2}\nالأقساط: {months}\nحالة الاستلام: {purchase.Status}", "تم الحفظ", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
        }
        catch (Exception ex) { MessageBox.Show(ArabicUi.ErrorMessage(ex, "تعذر حفظ الفاتورة. تحقق من البيانات وحاول مرة أخرى."), "تعذر حفظ الفاتورة", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private static bool Parse(string value, out decimal result) => decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out result) || decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
}
