using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SmartErpDesktop.Models;
using SmartErpDesktop.ViewModels;

namespace SmartErpDesktop.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel vm = new();
    private Button? activeNavigationButton;

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
            ("Representatives", "♙  المندوبون"),
            ("Collections", "ج.م  التحصيل والأقساط"),
            ("Products", "▦  المنتجات والمخزون"),
            ("Purchases", "⇄  المشتريات والموردون"),
            ("Stock", "▤  الجرد والتسويات"),
            ("Expenses", "−  المصاريف"),
            ("Settings", "⚙  الإعدادات")
        };
        items = items.Append(("CommissionRules", "قواعد العمولات")).ToArray();

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
                FontSize = 14,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            button.MouseEnter += (_, _) =>
            {
                if (!ReferenceEquals(button, activeNavigationButton))
                    button.Background = new SolidColorBrush(Color.FromRgb(48, 63, 82));
            };
            button.MouseLeave += (_, _) =>
            {
                if (!ReferenceEquals(button, activeNavigationButton))
                    button.Background = Brushes.Transparent;
            };
            button.Click += (_, _) => Navigate(key);
            NavigationPanel.Children.Add(button);
        }
    }

    private void Navigate(string page)
    {
        vm.CurrentPage = page;
        foreach (var button in NavigationPanel.Children.OfType<Button>())
        {
            bool selected = string.Equals(button.Tag?.ToString(), page, StringComparison.OrdinalIgnoreCase);
            button.FontWeight = selected ? FontWeights.SemiBold : FontWeights.Normal;
            button.Background = selected
                ? new SolidColorBrush(Color.FromRgb(30, 94, 145))
                : Brushes.Transparent;
            if (selected) activeNavigationButton = button;
        }
        PageTitleText.Text = vm.PageTitle;
        PageHost.Content = page switch
        {
            "Dashboard" => new DashboardView(vm.Service, Navigate),
            "QuickSale" => new QuickSaleView(vm.Service, Navigate),
            "Invoices" => new ModuleView("Invoices", vm.Service, Navigate),
            "Customers" => new ModuleView("Customers", vm.Service, Navigate),
            "Representatives" => new RepresentativesView(vm.Service, OpenRepresentativeDetails),
            "CommissionRules" => new CommissionRulesView(vm.Service),
            "RepresentativeDetails" => vm.SelectedRepresentative is { } representative
                ? new RepresentativeDetailsView(vm.Service, representative, () => Navigate("Representatives"))
                : new RepresentativesView(vm.Service, OpenRepresentativeDetails),
            "Collections" => new ModuleView("Collections", vm.Service, Navigate),
            "Products" => new ModuleView("Products", vm.Service, Navigate),
            "Purchases" => new ModuleView("Purchases", vm.Service, Navigate),
            "Stock" => new ModuleView("Stock", vm.Service, Navigate),
            "Expenses" => new ExpensesView(vm.Service),
            "Settings" => new SettingsView(
   ((App)Application.Current).CompanySettings),
            _ => new SimpleInfoView(
                "صفحة غير معروفة",
                "الصفحة المطلوبة غير متاحة.")
        };
    }

    private void OpenRepresentativeDetails(Representative representative)
    {
        vm.SelectedRepresentative = representative;
        Navigate("RepresentativeDetails");
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
