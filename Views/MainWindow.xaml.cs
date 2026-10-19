using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SmartErpDesktop.ViewModels;

namespace SmartErpDesktop.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel vm = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = vm;
        BuildNavigation();
        Navigate("Dashboard");
    }

    private void BuildNavigation()
    {
        var items = new[]
        {
            ("Dashboard", "⌂  لوحة العمل"),
            ("QuickSale", "＋  البيع السريع"),
            ("Invoices", "▤  الفواتير"),
            ("Customers", "♙  العملاء والتجار"),
            ("Collections", "ج.م  التحصيل والأقساط"),
            ("Products", "▦  المنتجات والمخزون"),
            ("Purchases", "⇄  المشتريات والموردون"),
            ("Stock", "▤  الجرد والتسويات"),
            ("Expenses", "−  المصاريف"),
            ("Reports", "▥  التقارير والأرباح"),
            ("Settings", "⚙  الإعدادات")
        };

        foreach (var (key, label) in items)
        {
            var button = new Button
            {
                Content = label,
                Tag = key,
                HorizontalContentAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10, 3, 10, 3),
                Padding = new Thickness(14, 12, 4, 4),//I added padding to the left to make the text more aligned with the icons 0,0
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                FontSize = 14
            };
            button.Click += (_, _) => Navigate(key);
            NavigationPanel.Children.Add(button);
        }
    }

    private void Navigate(string page)
    {
        vm.CurrentPage = page;
        PageTitleText.Text = vm.PageTitle;
        PageHost.Content = page switch
        {
            "Dashboard" => new DashboardView(vm.Service, Navigate),
            "QuickSale" => new QuickSaleView(vm.Service, Navigate),
            "Invoices" => new ModuleView("Invoices", vm.Service, Navigate),
            "Customers" => new ModuleView("Customers", vm.Service, Navigate),
            "Collections" => new ModuleView("Collections", vm.Service, Navigate),
            "Products" => new ModuleView("Products", vm.Service, Navigate),
            "Purchases" => new ModuleView("Purchases", vm.Service, Navigate),
            "Stock" => new ModuleView("Stock", vm.Service, Navigate),
            "Expenses" => new ExpensesView(vm.Service),
            "Reports" => new ReportsView(vm.Service),
            "Settings" => new SettingsView(
   ((App)Application.Current).CompanySettings),
            _ => new SimpleInfoView(
                "صفحة غير معروفة",
                "الصفحة المطلوبة غير متاحة.")
        };
    }

    private void GlobalSearch_Click(object sender, RoutedEventArgs e)
    {
        var query = GlobalSearchBox.Text.Trim();
        if (query.Length == 0)
        {
            MessageBox.Show("اكتب اسمًا أو هاتفًا أو كودًا للبحث.", "البحث", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var product = vm.Service.FindProduct(query);
        if (product is not null)
        {
            Navigate("Products");
            MessageBox.Show($"الصنف: {product.Name}\nالكود: {product.Code}\nالسعر: {product.Price:N2} ج.م\nالمتاح: {product.Stock}", "نتيجة البحث");
            return;
        }

        var customer = vm.Service.FindCustomer(query);
        if (customer is not null)
        {
            Navigate("Customers");
            MessageBox.Show($"العميل: {customer.Name}\nالهاتف: {customer.Phone}\nالرصيد: {customer.Balance:N2} ج.م", "نتيجة البحث");
            return;
        }

        var invoice = vm.Service.Invoices.FirstOrDefault(i => i.Number.Contains(query, StringComparison.OrdinalIgnoreCase));
        if (invoice is not null)
        {
            Navigate("Invoices");
            MessageBox.Show($"الفاتورة: {invoice.Number}\nالعميل: {invoice.CustomerName}\nالإجمالي: {invoice.Total:N2} ج.م", "نتيجة البحث");
            return;
        }

        MessageBox.Show("لا توجد نتائج مطابقة.", "البحث", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
