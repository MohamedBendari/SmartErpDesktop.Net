using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SmartErpDesktop.Models;
using SmartErpDesktop.Services;

namespace SmartErpDesktop.Views;

public partial class QuickSaleView : UserControl
{
    private readonly MockErpService service;
    private readonly Action<string> navigate;
    private readonly List<InvoiceLine> cart = new();
    private bool saved;

    public QuickSaleView(MockErpService s, Action<string> n)
    {
        InitializeComponent();
        service = s;
        navigate = n;
        CustomerBox.ItemsSource = service.Customers;
        CartGrid.ItemsSource = cart;
        UpdateTotals();
        ProductSearchBox.Focus();
    }

    private void ProductSearch_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) AddProduct();
    }

    private void AddProduct_Click(object sender, RoutedEventArgs e) => AddProduct();

    private void AddProduct()
    {
        var query = ProductSearchBox.Text.Trim();
        if (query.Length == 0) return;

        var product = service.FindProduct(query);
        if (product is null)
        {
            MessageBox.Show("لم يتم العثور على الصنف. تحقق من الاسم أو الكود أو الباركود.", "بحث الصنف", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var line = cart.FirstOrDefault(x => x.ProductCode == product.Code);
        if (line is not null)
        {
            if (line.Quantity + 1 > product.Stock)
            {
                MessageBox.Show($"المتاح بالمخزن {product.Stock} فقط.");
                return;
            }
            line.Quantity++;
        }
        else
        {
            if (product.Stock < 1)
            {
                MessageBox.Show("الصنف غير متاح بالمخزن.");
                return;
            }

            cart.Add(new InvoiceLine
            {
                ProductCode = product.Code,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = 1
            });
        }

        saved = false;
        RefreshCart();
        ProductSearchBox.Clear();
        ProductSearchBox.Focus();
    }

    private void RefreshCart()
    {
        CartGrid.ItemsSource = null;
        CartGrid.ItemsSource = cart;
        UpdateTotals();
    }



    private decimal Num(TextBox box)
    {
        if (box == null || string.IsNullOrWhiteSpace(box.Text))
            return 0;

        if (decimal.TryParse(
            box.Text,
            NumberStyles.Number,
            CultureInfo.CurrentCulture,
            out var value))
            return value;

        if (decimal.TryParse(
            box.Text,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out value))
            return value;

        return 0;
    }



    private void UpdateTotals()
    {
        if (TotalsText == null ||
            InvoiceDiscountBox == null ||
            PaidBox == null)
        {
            return;
        }

        var subtotal = cart.Sum(x => x.Total);
        var discount = Num(InvoiceDiscountBox);
        var total = Math.Max(0, subtotal - discount);
        var paid = Num(PaidBox);

        TotalsText.Text =
            $"{total:N2} / {Math.Max(0, total - paid):N2} ج.م";
    }


    private void Amounts_Changed(object sender, TextChangedEventArgs e) => UpdateTotals();

    private void CartGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                foreach (var item in cart)
                {
                    if (item.Quantity <= 0) item.Quantity = 1;
                    if (item.Discount < 0) item.Discount = 0;
                    var product = service.Products.First(x => x.Code == item.ProductCode);
                    if (item.Quantity > product.Stock) item.Quantity = product.Stock;
                }
                RefreshCart();
            }
            catch
            {
                // Keep the current cart if a cell edit cannot be parsed.
            }
        }));
    }

    private void RemoveLine_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is InvoiceLine line)
        {
            cart.Remove(line);
            RefreshCart();
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (saved)
        {
            MessageBox.Show("تم حفظ هذه العملية بالفعل. ابدأ فاتورة جديدة لمنع التكرار.");
            return;
        }

        try
        {
            var type = (SaleTypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "كاش";
            var invoice = service.SaveSale(cart, type, CustomerBox.SelectedItem as Customer, Num(PaidBox), Num(InvoiceDiscountBox));
            saved = true;
            var message = $"تم حفظ الفاتورة التجريبية {invoice.Number}\nالإجمالي: {invoice.Total:N2} ج.م\nالمتبقي: {invoice.Remaining:N2} ج.م\nتم تحديث المخزون التجريبي.";
            MessageBox.Show(message, "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            if (MessageBox.Show("هل تريد فتح الفواتير؟", "متابعة", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                navigate("Invoices");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "تعذر حفظ البيع", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        if (cart.Count == 0)
        {
            MessageBox.Show("أضف أصنافاً أولاً لمعاينة الفاتورة.");
            return;
        }

        var type = (SaleTypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "كاش";
        var total = Math.Max(0, cart.Sum(x => x.Total) - Num(InvoiceDiscountBox));
        var lines = string.Join("\n", cart.Select(x => $"{x.ProductName} × {x.Quantity} = {x.Total:N2} ج.م"));
        var text = $"SMART ERP — فاتورة تجريبية\n{DateTime.Now:yyyy/MM/dd HH:mm}\n{lines}\nالنوع: {type}\nالإجمالي: {total:N2} ج.م\nالمدفوع: {Num(PaidBox):N2} ج.م\nالمتبقي: {Math.Max(0, total - Num(PaidBox)):N2} ج.م\n\nمعاينة فقط — لا توجد طابعة مرتبطة.";

        var window = new Window
        {
            Title = "معاينة الفاتورة",
            Width = 500,
            Height = 600,
            FlowDirection = FlowDirection.RightToLeft,
            Content = new TextBox
            {
                Text = text,
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                Padding = new Thickness(24),
                FontSize = 16,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            }
        };
        window.ShowDialog();
    }
}
