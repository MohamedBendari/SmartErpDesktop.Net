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
        CustomerBox.ItemsSource = service.Customers.Where(c => c.IsActive).ToList();
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
        if (TotalsText == null || FinalTotalText == null ||
            InvoiceDiscountBox == null ||
            DiscountPercentBox == null ||
            PaidBox == null)
        {
            return;
        }
        var subtotal = cart.Sum(x => x.Total);
        var discount = Num(InvoiceDiscountBox);
        var percent = Num(DiscountPercentBox);
        var percentDiscount = decimal.Round(subtotal * percent / 100m, 2, MidpointRounding.AwayFromZero);
        var total = Math.Max(0, subtotal - percentDiscount - discount);
        var paid = Num(PaidBox);
        FinalTotalText.Text = $"{total:N2} ج.م";
        TotalsText.Text = $"قبل الخصم: {subtotal:N2} | الخصم: {(percentDiscount + discount):N2} | المدفوع: {paid:N2} | المتبقي: {Math.Max(0, total - paid):N2} ج.م";
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
            if (!decimal.TryParse(DiscountPercentBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out _) && !decimal.TryParse(DiscountPercentBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
                throw new InvalidOperationException("أدخل نسبة خصم رقمية صحيحة.");
            if (!decimal.TryParse(InvoiceDiscountBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out _) && !decimal.TryParse(InvoiceDiscountBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
                throw new InvalidOperationException("أدخل خصماً ثابتاً رقمياً صحيحاً.");
            var type = (SaleTypeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "كاش";
            List<Installment>? schedule = null;
            var subtotal = cart.Sum(x => x.Total);
            var pct = Num(DiscountPercentBox);
            if (pct < 0 || pct > 100) throw new InvalidOperationException("خصم النسبة يجب أن يكون بين 0 و100%.");
            if (Num(InvoiceDiscountBox) < 0) throw new InvalidOperationException("الخصم الثابت لا يمكن أن يكون سالباً.");
            var expectedTotal = Math.Max(0, subtotal - decimal.Round(subtotal * pct / 100m, 2, MidpointRounding.AwayFromZero) - Num(InvoiceDiscountBox));
            var expectedRemaining = expectedTotal - Num(PaidBox);
            var months = 0;
            if (type == "قسط أفراد" && (!int.TryParse(InstallmentMonthsBox.Text, out months) || months < 1 || months > 120))
                throw new InvalidOperationException("أدخل مدة تقسيط صحيحة من شهر إلى 120 شهراً.");

            // أدخل جدول الأقساط قبل حفظ الفاتورة حتى لا تُحفظ فاتورة قسط بلا جدول بسبب إلغاء النافذة.
            if (type == "قسط أفراد" && expectedRemaining > 0)
            {
                var scheduleWindow = new InstallmentScheduleWindow(expectedRemaining, months)
                {
                    Owner = Window.GetWindow(this)
                };
                if (scheduleWindow.ShowDialog() != true)
                    return;
                schedule = scheduleWindow.ResultInstallments;
            }

            var invoice = service.SaveSale(cart, type, CustomerBox.SelectedItem as Customer, Num(PaidBox), Num(InvoiceDiscountBox), pct, months, schedule);

            saved = true;
            var message = $"تم حفظ الفاتورة التجريبية {invoice.Number}\nالإجمالي: {invoice.Total:N2} ج.م\nالمتبقي: {invoice.Remaining:N2} ج.م\nتم تحديث المخزون التجريبي.";
            if (schedule is not null)
                message += $"\nعدد الأقساط: {schedule.Count}\nإجمالي جدول الأقساط: {schedule.Sum(x => x.Amount):N2} ج.م";
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
        SmartErpDesktop.Models.CompanySettings printSettings;
        try { printSettings = ((App)Application.Current).CompanySettings.GetSettings(); }
        catch { printSettings = new SmartErpDesktop.Models.CompanySettings(); }
        var doc = ErpPrintDocument.CreatePreviewInvoice(
            cart,
            type,
            CustomerBox.SelectedItem as SmartErpDesktop.Models.Customer,
            Num(DiscountPercentBox),
            Num(InvoiceDiscountBox),
            Num(PaidBox),
            printSettings);
        ErpPrintDocument.CreatePreviewWindow(
            doc, "معاينة فاتورة البيع", "معاينة فاتورة بيع",
            printSettings, Window.GetWindow(this))
            .ShowDialog();
    }
}
