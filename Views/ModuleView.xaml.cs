using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using SmartErpDesktop.Services;
using System.Linq;
using SmartErpDesktop.Models;
using System.Windows.Documents;
using System.Windows.Media;
namespace SmartErpDesktop.Views;
public partial class ModuleView : UserControl
{
    private readonly string module;
    private readonly MockErpService service;
    private readonly Action<string> navigate;
    private IEnumerable source = Array.Empty<object>();
    private bool showingSuppliers;
    public ModuleView(string key, MockErpService s, Action<string> n)
    {
        InitializeComponent();
        module = key;
        service = s;
        navigate = n;
        HeadingText.Text = key switch
        {
            "Invoices" => "الفواتير",
            "Customers" => "العملاء والتجار",
            "Collections" => "التحصيل والأقساط",
            "Products" => "المنتجات والمخزون",
            "Purchases" => "المشتريات والموردون",
            "Stock" => "الجرد وحركة المخزون",
            _ => key
        };
        PrimaryActionButton.Content = key switch
        {
            "Collections" => "تسجيل تحصيل",
            "Products" => "تعديل رصيد الجرد",
            "Customers" => "بدء بيع للعميل",
            "Invoices" => "فتح البيع السريع",
            _ => "إجراء جديد"
        };
        if (key == "Purchases")
        {
            PrimaryActionButton.Content = "فاتورة شراء جديدة";
            SupplierStatementButton.Visibility = Visibility.Visible;
            SupplierListButton.Visibility = AddSupplierButton.Visibility = EditSupplierButton.Visibility = DeleteSupplierButton.Visibility = SupplierInstallmentsButton.Visibility = Visibility.Visible;
        }
        if (key == "Products")
        {
            AddProductButton.Visibility = Visibility.Visible;
            EditProductButton.Visibility = Visibility.Visible;
            PriceProductButton.Visibility = Visibility.Visible;
            BulkPriceButton.Visibility = Visibility.Visible;
            BarcodeButton.Visibility = Visibility.Visible;
            StockAlertsButton.Visibility = Visibility.Visible;
            DataGridMain.SelectionMode = DataGridSelectionMode.Extended;
        }
        if (key == "Invoices")
        {
            InvoiceInstallmentsButton.Visibility = Visibility.Visible;
            FilterBox.ToolTip = "ابحث باسم العميل أو الشركة أو رقم الفاتورة أو نوع البيع";
        }
        if (key == "Customers")
        {
            AddTraderButton.Visibility = EditTraderButton.Visibility = DeleteTraderButton.Visibility = Visibility.Visible;
        }
        LoadData();
    }

    private Customer? GetSelectedTrader()
    {
        if (DataGridMain.SelectedItem is null) return null;
        var code = DataGridMain.SelectedItem.GetType().GetProperty("الكود")?.GetValue(DataGridMain.SelectedItem)?.ToString();
        return service.Customers.FirstOrDefault(c => c.Code == code && c.Type == "تاجر");
    }
    private void AddTrader_Click(object sender, RoutedEventArgs e)
    {
        var editor = new TraderEditorWindow(service) { Owner = Window.GetWindow(this) };
        if (editor.ShowDialog() == true) LoadData();
    }
    private void EditTrader_Click(object sender, RoutedEventArgs e)
    {
        var trader = GetSelectedTrader();
        if (trader is null) { MessageBox.Show("حدد تاجراً من القائمة."); return; }
        var editor = new TraderEditorWindow(service, trader) { Owner = Window.GetWindow(this) };
        if (editor.ShowDialog() == true) LoadData();
    }
    private void DeleteTrader_Click(object sender, RoutedEventArgs e)
    {
        var trader = GetSelectedTrader();
        if (trader is null) { MessageBox.Show("حدد تاجراً من القائمة."); return; }
        if (MessageBox.Show($"تأكيد حذف / أرشفة {trader.Name}؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try
        {
            bool archived = service.RemoveOrArchiveTrader(trader);
            LoadData();
            MessageBox.Show(archived ? "للتاجر معاملات مالية؛ تم أرشفته وإخفاؤه من العمليات الجديدة." : "تم حذف التاجر.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { MessageBox.Show(ArabicUi.ErrorMessage(ex, "تعذر حذف التاجر أو أرشفته."), "تعذر حذف التاجر", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
    private Product? GetSelectedProduct()
    {
        if (DataGridMain.SelectedItem is null) return null;
        var code = DataGridMain.SelectedItem.GetType().GetProperty("الكود")?.GetValue(DataGridMain.SelectedItem)?.ToString();
        return service.Products.FirstOrDefault(p => p.Code == code);
    }
    private List<Product> GetSelectedProducts()
    {
        return DataGridMain.SelectedItems.Cast<object>()
            .Select(item => item.GetType().GetProperty("الكود")?.GetValue(item)?.ToString())
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => service.Products.FirstOrDefault(p => p.Code == code))
            .Where(product => product is not null)
            .Cast<Product>().Distinct().ToList();
    }
    private void AddProduct_Click(object sender, RoutedEventArgs e)
    {
        var window = new ProductEditorWindow(service) { Owner = Window.GetWindow(this) };
        if (window.ShowDialog() == true) { LoadData(); FilterBox.Clear(); }
    }
    private void EditProduct_Click(object sender, RoutedEventArgs e)
    {
        var product = GetSelectedProduct();
        if (product is null) { MessageBox.Show("حدد صنفاً واحداً لتعديله."); return; }
        var window = new ProductEditorWindow(service, product) { Owner = Window.GetWindow(this) };
        if (window.ShowDialog() == true) LoadData();
    }
    private void PriceProduct_Click(object sender, RoutedEventArgs e)
    {
        var product = GetSelectedProduct();
        if (product is null) { MessageBox.Show("حدد صنفاً واحداً لتعديل سعره."); return; }
        var value = Prompt($"السعر الجديد للصنف {product.Name}:", product.Price.ToString("0.##", CultureInfo.InvariantCulture));
        if (value is null) return;
        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var price) &&
            !decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out price))
        { MessageBox.Show("أدخل سعراً رقمياً صحيحاً."); return; }
        if (price < 0) { MessageBox.Show("السعر لا يمكن أن يكون بالسالب."); return; }
        product.Price = price;
        service.SaveChanges();
        LoadData();
        MessageBox.Show("تم تحديث سعر الصنف.", "تحديث السعر", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    private void BulkPrice_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedProducts();
        if (selected.Count == 0) { MessageBox.Show("حدد صنفاً واحداً أو أكثر. استخدم Ctrl أو Shift للتحديد المتعدد."); return; }
        var mode = MessageBox.Show("هل تريد زيادة الأسعار بنسبة مئوية؟\nنعم = نسبة مئوية، لا = إضافة مبلغ ثابت، إلغاء = رجوع.", "تحديث جماعي للأسعار", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        if (mode == MessageBoxResult.Cancel) return;
        var input = Prompt(mode == MessageBoxResult.Yes ? "نسبة الزيادة المئوية:" : "المبلغ المضاف لكل سعر:", "0");
        if (input is null) return;
        if (!decimal.TryParse(input, NumberStyles.Number, CultureInfo.CurrentCulture, out var amount) &&
            !decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
        { MessageBox.Show("أدخل قيمة رقمية صحيحة."); return; }
        try
        {
            var count = service.UpdatePrices(selected.Select(p => p.Code), amount, mode == MessageBoxResult.Yes);
            LoadData();
            MessageBox.Show($"تم تحديث أسعار {count} صنف/أصناف.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { MessageBox.Show(ArabicUi.ErrorMessage(ex, "تعذر تحديث الأسعار. تحقق من البيانات وحاول مرة أخرى."), "تعذر تحديث الأسعار", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
    private void Barcode_Click(object sender, RoutedEventArgs e)
    {
        var product = GetSelectedProduct();
        if (product is null)
        {
            MessageBox.Show("حدد صنفاً واحداً أولاً.", "الباركود",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (string.IsNullOrWhiteSpace(product.Barcode))
        {
            product.Barcode = service.GenerateUniqueBarcode();
            service.SaveChanges();
            LoadData();
        }
        try
        {
            var window = new BarcodeLabelWindow(product) { Owner = Window.GetWindow(this) };
            window.ShowDialog();
        }
        catch (Exception ex)
        {
                MessageBox.Show(ArabicUi.ErrorMessage(ex, "تعذر تجهيز ملصق الباركود."), "تعذر تجهيز الباركود",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void StockAlerts_Click(object sender, RoutedEventArgs e)
    {
        var lowStock = service.LowStockProducts
            .OrderBy(p => p.Stock)
            .Select(p => new
            {
                الكود = p.Code,
                الصنف = p.Name,
                الباركود = p.Barcode,
                المتاح = p.Stock,
                حد_التنبيه = p.MinimumStock,
                الحالة = p.Stock <= 0 ? "نفد المخزون" : "مخزون منخفض",
                النقص = Math.Max(0, p.MinimumStock - p.Stock)
            }).ToList();
        var window = new Window
        {
            Title = "تنبيهات نقص المخزون",
            Width = 760,
            Height = 460,
            MinWidth = 600,
            MinHeight = 320,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Window.GetWindow(this),
            FlowDirection = FlowDirection.RightToLeft
        };
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var summary = new TextBlock
        {
            Text = lowStock.Count == 0 ? "لا توجد أصناف منخفضة المخزون حالياً." : $"عدد الأصناف التي تحتاج متابعة: {lowStock.Count}",
            Margin = new Thickness(12),
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = lowStock.Count == 0 ? Brushes.SeaGreen : Brushes.Firebrick
        };
        Grid.SetRow(summary, 0); grid.Children.Add(summary);
        var table = new DataGrid
        {
            ItemsSource = lowStock,
            AutoGenerateColumns = true,
            IsReadOnly = true,
            Margin = new Thickness(12, 0, 12, 12),
            CanUserAddRows = false
        };
        Grid.SetRow(table, 1); grid.Children.Add(table);
        var close = new Button
        {
            Content = "إغلاق",
            Padding = new Thickness(18, 7, 18, 7),
            Margin = new Thickness(12),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        close.Click += (_, _) => window.Close();
        Grid.SetRow(close, 2); grid.Children.Add(close);
        window.Content = grid;
        window.ShowDialog();
    }

    private void LoadData()
    {
        object data = module switch
        {
            "Invoices" => service.Invoices.Select(i => new { رقم_الفاتورة = i.Number, التاريخ = i.Date.ToString("yyyy/MM/dd HH:mm"), العميل = i.CustomerName, المندوب = string.IsNullOrWhiteSpace(i.RepresentativeName) ? i.RepresentativeCode : i.RepresentativeName, نوع_البيع = i.Type, الإجمالي = i.Total, المدفوع = i.Paid, المتبقي = i.Remaining, الحالة = i.Status }).ToList(),
            "Customers" => service.Customers.Select(c => new { الكود = c.Code, الاسم = c.Name, الهاتف = c.Phone, العنوان = c.Address, النوع = c.Type, الرصيد = c.Balance, نشط = c.IsActive }).ToList(),
            "Collections" => service.Installments.OrderBy(x => x.DueDate).Select(x => new
            {
                معرّف_القسط = x.Id,
                الفاتورة = x.InvoiceNumber,
                القسط = x.Sequence,
                العميل = x.CustomerName,
                تاريخ_الاستحقاق = x.DueDate.ToString("yyyy/MM/dd"),
                قيمة_القسط = x.Amount,
                المدفوع = x.PaidAmount,
                المتبقي = x.Remaining,
                الحالة = x.Status
            }).ToList(),
            "Products" => service.Products.Select(p => new { الكود = p.Code, الباركود = p.Barcode, الصنف = p.Name, التصنيف = p.Category, سعر_البيع = p.Price, التكلفة = p.Cost, المتاح = p.Stock, حد_التنبيه = p.MinimumStock, الحالة = p.Stock <= p.MinimumStock ? "مخزون منخفض" : "متاح" }).ToList(),
            "Purchases" => service.Purchases.Select(p => new { رقم_الشراء = p.Number, التاريخ = p.Date.ToString("yyyy/MM/dd"), المورد = p.Supplier, عدد_السطور = p.Lines, الإجمالي = p.Total, المدفوع = p.Paid, المتبقي = p.Remaining, الحالة = p.Status }).ToList(),
            "Stock" => service.Movements.Select(m => new { التاريخ = m.Date.ToString("yyyy/MM/dd HH:mm"), الكود = m.ProductCode, الصنف = m.ProductName, نوع_الحركة = m.Type, الكمية = m.Quantity, السبب = m.Reason, المستخدم = m.User }).ToList(),
            _ => new List<object>()
        };
        if (module == "Purchases" && showingSuppliers)
            data = service.Suppliers.Where(s => s.IsActive).Select(s => new { الكود = s.Code, الاسم = s.Name, الهاتف = s.Phone, العنوان = s.Address, الرصيد = s.Balance, نشط = s.IsActive }).ToList();
        source = (IEnumerable)data;
        DataGridMain.ItemsSource = source;
        if (module == "Collections")
        {
            var openInstallments = service.Installments.Count(x => x.Remaining > 0);
            SubheadingText.Text = $"عدد الأقساط: {service.Installments.Count} | أقساط عليها متبقي: {openInstallments} | اختر قسطاً ثم اضغط تسجيل تحصيل";
            SubheadingText.Foreground = openInstallments > 0 ? Brushes.Firebrick : Brushes.SeaGreen;
        }
        if (module == "Products")
        {
            int low = service.LowStockProducts.Count;
            SubheadingText.Text = $"عدد الأصناف: {service.Products.Count} | منخفض/منتهي: {low} | بيانات تجريبية غير محفوظة بعد إغلاق التطبيق";
            SubheadingText.Foreground = low > 0 ? Brushes.Firebrick : Brushes.SeaGreen;
        }
    }
    private void Filter_Click(object sender, RoutedEventArgs e)
    {
        var query = FilterBox.Text.Trim();
        if (query.Length == 0)
        {
            LoadData();
            return;
        }
        var rows = new List<object>();
        foreach (var item in source)
        {
            if (item is not null && item.GetType().GetProperties().Any(p => (p.GetValue(item)?.ToString() ?? "").Contains(query, StringComparison.OrdinalIgnoreCase)))
                rows.Add(item);
        }
        DataGridMain.ItemsSource = rows;
        if (rows.Count == 0) MessageBox.Show("لا توجد نتائج مطابقة.", "البحث");
    }
    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        FilterBox.Clear();
        LoadData();
    }
    private void SupplierStatement_Click(object sender, RoutedEventArgs e)
    {
        var window = new SupplierStatementWindow(service) { Owner = Window.GetWindow(this) };
        window.ShowDialog();
        LoadData();
    }

    private Supplier? GetSelectedSupplier()
    {
        var code = DataGridMain.SelectedItem?.GetType().GetProperty("الكود")?.GetValue(DataGridMain.SelectedItem)?.ToString();
        return service.Suppliers.FirstOrDefault(x => x.Code == code);
    }
    private void SupplierList_Click(object sender, RoutedEventArgs e) { showingSuppliers = !showingSuppliers; SupplierListButton.Content = showingSuppliers ? "عرض المشتريات" : "قائمة الموردين"; LoadData(); }
    private void AddSupplier_Click(object sender, RoutedEventArgs e) { var w = new SupplierEditorWindow(service) { Owner = Window.GetWindow(this) }; if (w.ShowDialog() == true) { showingSuppliers = true; LoadData(); } }
    private void EditSupplier_Click(object sender, RoutedEventArgs e) { var s = GetSelectedSupplier(); if (s is null) { MessageBox.Show("حدد مورداً من القائمة أولاً."); return; } var w = new SupplierEditorWindow(service, s) { Owner = Window.GetWindow(this) }; if (w.ShowDialog() == true) LoadData(); }
    private void DeleteSupplier_Click(object sender, RoutedEventArgs e) { var s = GetSelectedSupplier(); if (s is null) { MessageBox.Show("حدد مورداً من القائمة أولاً."); return; } if (MessageBox.Show($"حذف أو أرشفة المورد {s.Name}؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return; var archived = service.RemoveOrArchiveSupplier(s); LoadData(); MessageBox.Show(archived ? "للمورد سجل مالي؛ تم أرشفته." : "تم حذف المورد."); }
    private void SupplierInstallments_Click(object sender, RoutedEventArgs e)
    {
        Supplier? supplier; string? purchaseNumber = null;
        if (showingSuppliers) supplier = GetSelectedSupplier();
        else
        {
            var values = DataGridMain.SelectedItem?.GetType().GetProperties().Select(p => p.GetValue(DataGridMain.SelectedItem)?.ToString()).Where(v => !string.IsNullOrWhiteSpace(v)).ToList() ?? new List<string?>();
            var purchase = service.Purchases.FirstOrDefault(p => values.Contains(p.Number)); supplier = purchase is null ? null : service.Suppliers.FirstOrDefault(s => s.Name == purchase.Supplier); purchaseNumber = purchase?.Number;
        }
        if (supplier is null) { MessageBox.Show(showingSuppliers ? "حدد مورداً من قائمة الموردين أولاً." : "حدد فاتورة شراء أو مورداً أولاً."); return; }
        new SupplierInstallmentWindow(service, supplier, purchaseNumber) { Owner = Window.GetWindow(this) }.ShowDialog(); LoadData();
    }

    private void PrimaryAction_Click(object sender, RoutedEventArgs e)
    {
        if (module == "Purchases")
        {
            var window = new PurchaseEditorWindow(service) { Owner = Window.GetWindow(this) };
            if (window.ShowDialog() == true) LoadData();
            return;
        }
        if (module == "Collections")
        {
            if (DataGridMain.SelectedItem is null)
            {
                MessageBox.Show("حدد القسط الذي تريد تسجيل تحصيل عليه من الجدول أولاً.", "اختيار القسط", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var installmentId = DataGridMain.SelectedItem.GetType().GetProperty("معرّف_القسط")?.GetValue(DataGridMain.SelectedItem)?.ToString();
            var installment = service.Installments.FirstOrDefault(x => x.Id == installmentId);
            if (installment is null)
            {
                MessageBox.Show("لم يتم العثور على القسط المحدد. اضغط تحديث وحاول مرة أخرى.", "القسط غير موجود", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var window = new InstallmentCollectionWindow(service, installment) { Owner = Window.GetWindow(this) };
            if (window.ShowDialog() == true) LoadData();
        }
        else if (module == "Products")
        {
            var product = GetSelectedProduct();
            if (product is null)
            {
                MessageBox.Show("حدد صنفاً من الجدول أولاً.", "تسوية الجرد",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var actual = Prompt("أدخل الكمية الفعلية بعد الجرد:", product.Stock.ToString("0.##", CultureInfo.CurrentCulture));
            if (actual is null) return;
            if (!decimal.TryParse(actual, NumberStyles.Number, CultureInfo.CurrentCulture, out var quantity) &&
                !decimal.TryParse(actual, NumberStyles.Number, CultureInfo.InvariantCulture, out quantity))
            {
                MessageBox.Show("أدخل كمية فعلية صحيحة.", "تسوية الجرد",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var reason = Prompt("سبب التسوية:", "جرد فعلي");
            if (string.IsNullOrWhiteSpace(reason)) return;

            try
            {
                service.AdjustStock(product, quantity, reason.Trim());
                LoadData();
                MessageBox.Show("تم تحديث الكمية وإضافة سجل حركة.", "نجاح",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ArabicUi.ErrorMessage(ex, "تعذر إجراء التسوية. راجع البيانات وحاول مرة أخرى."), "تعذر إجراء التسوية",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        else if (module == "Stock")
        {
            // شاشة الجرد تعرض حركات المخزون، لذلك نطلب تحديد الصنف بالكود أو الباركود أو الاسم.
            var query = Prompt("أدخل كود الصنف أو الباركود أو اسمه لإجراء تسوية جرد:", "");
            if (string.IsNullOrWhiteSpace(query)) return;

            var product = service.FindProduct(query);
            if (product is null)
            {
                MessageBox.Show("لم يتم العثور على صنف مطابق. راجع الكود أو الباركود أو الاسم.",
                    "الصنف غير موجود", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var actual = Prompt($"الصنف: {product.Name}\nالرصيد المسجل: {product.Stock}\nأدخل الكمية الفعلية:", product.Stock.ToString("0.##", CultureInfo.CurrentCulture));
            if (actual is null) return;
            if (!decimal.TryParse(actual, NumberStyles.Number, CultureInfo.CurrentCulture, out var quantity) &&
                !decimal.TryParse(actual, NumberStyles.Number, CultureInfo.InvariantCulture, out quantity))
            {
                MessageBox.Show("أدخل كمية فعلية صحيحة.", "تسوية الجرد",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var reason = Prompt("سبب فرق الجرد:", "جرد فعلي");
            if (string.IsNullOrWhiteSpace(reason)) return;

            try
            {
                service.AdjustStock(product, quantity, reason.Trim());
                LoadData();
                MessageBox.Show($"تمت تسوية الصنف {product.Name} وتسجيل الفرق والسبب في حركة المخزون.",
                    "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ArabicUi.ErrorMessage(ex, "تعذر إجراء التسوية. راجع البيانات وحاول مرة أخرى."), "تعذر إجراء التسوية",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        else if (module == "Customers")
        {
            navigate("QuickSale");
        }
        else if (module == "Invoices")
        {
            navigate("QuickSale");
        }
        else
        {
            MessageBox.Show("إنشاء مستندات الشراء يحتاج اعتماد حقول وتدفق فاتورة المورد؛ هذه النسخة تعرض بيانات تجريبية فقط.");
        }
    }
    private string? Prompt(string label, string initial)
    {
        var window = new Window
        {
            Title = "إدخال بيانات",
            Width = 400,
            Height = 190,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Window.GetWindow(this),
            FlowDirection = FlowDirection.RightToLeft
        };
        var panel = new StackPanel { Margin = new Thickness(14) };
        panel.Children.Add(new TextBlock { Text = label });
        var box = new TextBox { Text = initial };
        panel.Children.Add(box);
        var button = new Button { Content = "تأكيد", HorizontalAlignment = HorizontalAlignment.Left };
        button.Click += (_, _) => window.DialogResult = true;
        panel.Children.Add(button);
        window.Content = panel;
        return window.ShowDialog() == true ? box.Text : null;
    }
    private void Details_Click(object sender, RoutedEventArgs e)
    {
        if (DataGridMain.SelectedItem is null)
        {
            MessageBox.Show("حدد صفاً لعرض التفاصيل.");
            return;
        }

        if (module == "Customers")
        {
            var code = DataGridMain.SelectedItem.GetType().GetProperty("الكود")?.GetValue(DataGridMain.SelectedItem)?.ToString();
            var name = DataGridMain.SelectedItem.GetType().GetProperty("الاسم")?.GetValue(DataGridMain.SelectedItem)?.ToString();
            var customer = service.Customers.FirstOrDefault(c =>
                (!string.IsNullOrWhiteSpace(code) && c.Code == code) ||
                (!string.IsNullOrWhiteSpace(name) && c.Name == name));
            if (customer is null)
            {
                MessageBox.Show("تعذر تحديد العميل من السجل المحدد.", "كشف حساب العميل");
                return;
            }
            new CustomerStatementWindow(service, customer) { Owner = Window.GetWindow(this) }.ShowDialog();
            return;
        }

        if (module == "Invoices")
        {
            var number = DataGridMain.SelectedItem.GetType().GetProperty("رقم_الفاتورة")?.GetValue(DataGridMain.SelectedItem)?.ToString();
            var invoice = service.Invoices.FirstOrDefault(i => i.Number == number);
            if (invoice is null) { MessageBox.Show("تعذر تحديد الفاتورة."); return; }
            var rows = invoice.Lines.Select(l => $"{l.ProductName} | الكمية: {l.Quantity:N2} | سعر الوحدة: {l.UnitPrice:N2} | الإجمالي: {l.Total:N2}").ToList();
            var representativeName = InvoiceReceiptDocument.GetRepresentativeDisplayName(invoice);
            if (!string.IsNullOrWhiteSpace(representativeName))
                rows.Add($"المندوب: {representativeName}");
            rows.Insert(0, $"الفاتورة: {invoice.Number} | التاريخ: {invoice.Date:yyyy/MM/dd HH:mm} | العميل/الشركة: {invoice.CustomerName} | النوع: {invoice.Type}");
            rows.Add($"الإجمالي: {invoice.Total:N2} | المدفوع: {invoice.Paid:N2} | المتبقي: {invoice.Remaining:N2}");
            var installments = service.GetInstallments(invoice.Number);
            if (installments.Count > 0)
            {
                rows.Add("--- جدول الأقساط ---");
                rows.AddRange(installments.Select(x => $"القسط {x.Sequence} | الاستحقاق: {x.DueDate:yyyy/MM/dd} | القيمة: {x.Amount:N2} | المدفوع: {x.PaidAmount:N2} | المتبقي: {x.Remaining:N2} | الحالة: {x.Status}"));
            }
            ShowInvoiceDetails(string.Join("\n", rows), invoice.Number);
            return;
        }

        var details = string.Join("\n", DataGridMain.SelectedItem.GetType().GetProperties()
            .Select(property => $"{property.Name}: {property.GetValue(DataGridMain.SelectedItem)}"));
        MessageBox.Show(details, "تفاصيل السجل");
    }
    private void InvoiceInstallments_Click(object sender, RoutedEventArgs e)
    {
        if (module != "Invoices" || DataGridMain.SelectedItem is null) { MessageBox.Show("حدد فاتورة أولاً."); return; }
        var number = DataGridMain.SelectedItem.GetType().GetProperty("رقم_الفاتورة")?.GetValue(DataGridMain.SelectedItem)?.ToString();
        var invoice = service.Invoices.FirstOrDefault(i => i.Number == number);
        if (invoice is null) { MessageBox.Show("تعذر تحديد الفاتورة."); return; }
        var items = service.GetInstallments(invoice.Number);
        if (items.Count == 0) { MessageBox.Show(invoice.Type == "قسط أفراد" ? "لا يوجد جدول أقساط مسجل لهذه الفاتورة." : "الفاتورة ليست بيع تقسيط."); return; }
        var text = string.Join("\n", items.Select(x => $"القسط {x.Sequence} | تاريخ الاستحقاق: {x.DueDate:yyyy/MM/dd} | القيمة: {x.Amount:N2} | المدفوع: {x.PaidAmount:N2} | المتبقي: {x.Remaining:N2} | الحالة: {x.Status}"));
        ShowInvoiceDetails(text, "أقساط " + invoice.Number);
    }

    private void ShowInvoiceDetails(string text, string title)
    {
        var window = new Window { Title = title, Width = 760, Height = 520, Owner = Window.GetWindow(this), FlowDirection = FlowDirection.RightToLeft, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var box = new TextBox { Text = text, IsReadOnly = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(18), FontSize = 14 };
        window.Content = box; window.ShowDialog();
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        if (DataGridMain.SelectedItem is null)
        {
            MessageBox.Show("حدد سجلاً للمعاينة.", "معاينة الطباعة",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        bool isInvoice = module == "Invoices";
        object selected = DataGridMain.SelectedItem;
        string GetValue(string propertyName) =>
            selected.GetType().GetProperty(propertyName)?.GetValue(selected)?.ToString() ?? "";
        Invoice? invoice = null;
        if (isInvoice)
        {
            string invoiceNumber = GetValue("رقم_الفاتورة");
            invoice = service.Invoices.FirstOrDefault(x => x.Number == invoiceNumber);
            if (invoice is null)
            {
                MessageBox.Show("لم يتم العثور على الفاتورة الأصلية.", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }
        CompanySettings settings;
        try
        {
            settings = ((App)Application.Current).CompanySettings.GetSettings();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ArabicUi.ErrorMessage(ex, "تعذر تحميل إعدادات المنشأة من قاعدة البيانات."),
                "إعدادات المنشأة", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        if (settings.ShowStoreInfo && string.IsNullOrWhiteSpace(settings.StoreName))
        {
            MessageBox.Show(
                "اسم المنشأة غير مسجل أو لم يتم حفظه. افتح الإعدادات، أدخل اسم المنشأة، ثم اضغط حفظ.",
                "بيانات المنشأة", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        Customer? invoiceCustomer = invoice is null ? null : service.Customers.FirstOrDefault(x =>
            (!string.IsNullOrWhiteSpace(invoice.CustomerCode) && x.Code.Equals(invoice.CustomerCode, StringComparison.OrdinalIgnoreCase)) ||
            (string.IsNullOrWhiteSpace(invoice.CustomerCode) && x.Name.Equals(invoice.CustomerName, StringComparison.OrdinalIgnoreCase)));
        IReadOnlyList<Installment> invoiceInstallments = invoice is null ? Array.Empty<Installment>() : service.GetInstallments(invoice.Number);
        var document = isInvoice && invoice is not null
            ? InvoiceReceiptDocument.Create(invoice, invoiceCustomer, settings, invoiceInstallments)
            : new FlowDocument(new Paragraph(new Run("معاينة السجل"))) { FlowDirection = FlowDirection.RightToLeft };
        if (!isInvoice) document = ErpPrintDocument.CreateRecordDocument(HeadingText.Text, selected, settings);
        void PrintDocument()
        {
            try
            {
                var dialog = new PrintDialog();
                try
                {
                    if (!string.IsNullOrWhiteSpace(settings.PrinterName))
                    {
                        var configuredQueue = new System.Printing.LocalPrintServer().GetPrintQueues()
                            .FirstOrDefault(q => q.FullName.Equals(settings.PrinterName, StringComparison.OrdinalIgnoreCase));
                        if (configuredQueue is not null) dialog.PrintQueue = configuredQueue;
                    }
                    var ticket = dialog.PrintTicket ?? new System.Printing.PrintTicket();
                    double paperWidth = settings.PaperSize.Contains("58") ? 219.2 : settings.PaperSize.Equals("A4", StringComparison.OrdinalIgnoreCase) ? 793.7 : 302.4;
                    double paperHeight = settings.PaperSize.Equals("A4", StringComparison.OrdinalIgnoreCase) ? 1122.5 : 720;
                    ticket.PageMediaSize = new System.Printing.PageMediaSize(paperWidth, paperHeight);
                    ticket.PageOrientation = System.Printing.PageOrientation.Portrait;
                    dialog.PrintTicket = ticket;
                }
                catch { /* Keep selected printer defaults if a driver rejects the paper setting. */ }
                if (dialog.ShowDialog() == true)
                {
                    IDocumentPaginatorSource printable = document;
                    dialog.PrintDocument(printable.DocumentPaginator,
                        isInvoice ? "فاتورة " + invoice!.Number : "معاينة سجل");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ArabicUi.ErrorMessage(ex, "تعذرت الطباعة. تحقق من إعدادات الطابعة وحاول مرة أخرى."),
                    "خطأ في الطباعة", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        if (!settings.ShowPrintPreview)
        {
            PrintDocument();
            return;
        }
        var viewer = new FlowDocumentScrollViewer
        {
            Document = document,
            IsToolBarVisible = false,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = Brushes.White,
            Padding = new Thickness(12)
        };
        var printButton = new Button
        {
            Content = "طباعة الفاتورة",
            Padding = new Thickness(18, 10, 18, 10),
            Margin = new Thickness(8),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        var layout = new DockPanel();
        DockPanel.SetDock(printButton, Dock.Bottom);
        layout.Children.Add(printButton);
        layout.Children.Add(viewer);
        var window = new Window
        {
            Title = isInvoice ? "معاينة فاتورة المبيعات" : "معاينة السجل",
            Width = settings.PaperSize.Equals("A4", StringComparison.OrdinalIgnoreCase) ? 850 : 480,
            Height = 760,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Window.GetWindow(this),
            FlowDirection = FlowDirection.RightToLeft,
            Background = Brushes.White,
            Content = layout
        };
        printButton.Click += (_, _) => PrintDocument();
        window.ShowDialog();
    }

    private FlowDocument CreateSelectedRecordDocument(object selected)
    {
        var doc = new FlowDocument { FlowDirection = FlowDirection.RightToLeft, PagePadding = new Thickness(40), ColumnWidth = 700 };
        doc.Blocks.Add(new Paragraph(new Run(HeadingText.Text)) { FontSize = 22, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center });
        doc.Blocks.Add(new Paragraph(new Run("سمارت لإدارة الأعمال")) { FontSize = 13, TextAlignment = TextAlignment.Center });
        foreach (var property in selected.GetType().GetProperties())
        {
            var value = property.GetValue(selected);
            doc.Blocks.Add(new Paragraph(new Run($"{property.Name.Replace('_', ' ')}: {value ?? "—"}")) { Margin = new Thickness(0, 5, 0, 5), FontSize = 14 });
        }
        doc.Blocks.Add(new Paragraph(new Run($"تاريخ الطباعة: {DateTime.Now:yyyy/MM/dd HH:mm}")) { Margin = new Thickness(0, 20, 0, 0), FontSize = 10 });
        return doc;
    }
}
