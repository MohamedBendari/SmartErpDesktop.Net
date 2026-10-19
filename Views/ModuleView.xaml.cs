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
        LoadData();
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
        catch (Exception ex) { MessageBox.Show(ex.Message, "تعذر تحديث الأسعار", MessageBoxButton.OK, MessageBoxImage.Warning); }
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
            MessageBox.Show(ex.Message, "تعذر تجهيز الباركود",
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
            "Invoices" => service.Invoices.Select(i => new { رقم_الفاتورة = i.Number, التاريخ = i.Date.ToString("yyyy/MM/dd HH:mm"), العميل = i.CustomerName, نوع_البيع = i.Type, الإجمالي = i.Total, المدفوع = i.Paid, المتبقي = i.Remaining, الحالة = i.Status }).ToList(),
            "Customers" => service.Customers.Select(c => new { الكود = c.Code, الاسم = c.Name, الهاتف = c.Phone, العنوان = c.Address, النوع = c.Type, الرصيد = c.Balance }).ToList(),
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
            "Purchases" => service.Purchases.Select(p => new { رقم_الشراء = p.Number, التاريخ = p.Date.ToString("yyyy/MM/dd"), المورد = p.Supplier, عدد_السطور = p.Lines, الإجمالي = p.Total, الحالة = p.Status }).ToList(),
            "Stock" => service.Movements.Select(m => new { التاريخ = m.Date.ToString("yyyy/MM/dd HH:mm"), الكود = m.ProductCode, الصنف = m.ProductName, نوع_الحركة = m.Type, الكمية = m.Quantity, السبب = m.Reason, المستخدم = m.User }).ToList(),
            _ => new List<object>()
        };
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
            if (item?.ToString()?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)
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
    private void PrimaryAction_Click(object sender, RoutedEventArgs e)
    {
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
                MessageBox.Show(ex.Message, "تعذر إجراء التسوية",
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
                MessageBox.Show(ex.Message, "تعذر إجراء التسوية",
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

        var details = string.Join("\n", DataGridMain.SelectedItem.GetType().GetProperties()
            .Select(property => $"{property.Name}: {property.GetValue(DataGridMain.SelectedItem)}"));
        MessageBox.Show(details, "تفاصيل السجل");
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
                "تعذر تحميل إعدادات المنشأة من قاعدة البيانات.\n" + ex.Message,
                "إعدادات المنشأة", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        if (settings.ShowStoreInfo && string.IsNullOrWhiteSpace(settings.StoreName))
        {
            MessageBox.Show(
                "اسم المنشأة غير مسجل أو لم يتم حفظه. افتح الإعدادات، أدخل اسم المنشأة، ثم اضغط حفظ.",
                "بيانات المنشأة", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        var document = new FlowDocument
        {
            PagePadding = new Thickness(30),
            ColumnWidth = double.PositiveInfinity,
            FontFamily = new FontFamily("Arial"),
            FontSize = 12,
            FlowDirection = FlowDirection.RightToLeft
        };
        void AddParagraph(string text, double size = 12, bool bold = false,
            TextAlignment alignment = TextAlignment.Right)
        {
            document.Blocks.Add(new Paragraph(new Run(text))
            {
                FontSize = size,
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                TextAlignment = alignment,
                Margin = new Thickness(0, 4, 0, 4)
            });
        }
        if (settings.ShowStoreInfo)
        {
            AddParagraph(
                string.IsNullOrWhiteSpace(settings.StoreName)
                    ? "اسم المنشأة غير محدد"
                    : settings.StoreName,
                22, true, TextAlignment.Center);
            if (!string.IsNullOrWhiteSpace(settings.Phone))
                AddParagraph("الهاتف: " + settings.Phone, 11, false, TextAlignment.Center);
            if (!string.IsNullOrWhiteSpace(settings.Address))
                AddParagraph("العنوان: " + settings.Address, 11, false, TextAlignment.Center);
            if (!string.IsNullOrWhiteSpace(settings.TaxNumber))
                AddParagraph("الرقم الضريبي: " + settings.TaxNumber, 11, false, TextAlignment.Center);
        }
        AddParagraph(isInvoice ? "فاتورة مبيعات" : "معاينة سجل", 20, true, TextAlignment.Center);
        if (isInvoice && invoice is not null)
        {
            AddParagraph("رقم الفاتورة: " + invoice.Number);
            AddParagraph("التاريخ: " + invoice.Date.ToString("yyyy/MM/dd HH:mm"));
            AddParagraph("العميل: " + invoice.CustomerName);
            AddParagraph("نوع البيع: " + invoice.Type);
            AddParagraph("تفاصيل الأصناف", 15, true);
            var table = new Table { CellSpacing = 0 };
            table.Columns.Add(new TableColumn { Width = new GridLength(3, GridUnitType.Star) });
            table.Columns.Add(new TableColumn { Width = new GridLength(1.2, GridUnitType.Star) });
            table.Columns.Add(new TableColumn { Width = new GridLength(1.5, GridUnitType.Star) });
            table.Columns.Add(new TableColumn { Width = new GridLength(1.3, GridUnitType.Star) });
            table.Columns.Add(new TableColumn { Width = new GridLength(1.6, GridUnitType.Star) });
            var rowGroup = new TableRowGroup();
            table.RowGroups.Add(rowGroup);
            void AddTableRow(params string[] values)
            {
                var row = new TableRow();
                foreach (string value in values)
                {
                    row.Cells.Add(new TableCell(new Paragraph(new Run(value))
                    {
                        Margin = new Thickness(3),
                        TextAlignment = TextAlignment.Center
                    })
                    {
                        BorderBrush = Brushes.Gray,
                        BorderThickness = new Thickness(0.5),
                        Padding = new Thickness(3)
                    });
                }
                rowGroup.Rows.Add(row);
            }
            AddTableRow("الصنف", "الكمية", "سعر الوحدة", "الخصم", "الإجمالي");
            foreach (InvoiceLine line in invoice.Lines)
            {
                AddTableRow(
                    line.ProductName,
                    line.Quantity.ToString("N2"),
                    line.UnitPrice.ToString("N2") + " ج.م",
                    line.Discount.ToString("N2") + " ج.م",
                    line.Total.ToString("N2") + " ج.م");
            }
            document.Blocks.Add(table);
            decimal itemsTotal = invoice.Lines.Sum(line => line.Total);
            decimal invoiceDiscount = Math.Max(0, itemsTotal - invoice.Total);
            AddParagraph("إجمالي البنود: " + itemsTotal.ToString("N2") + " ج.م", 12, true);
            if (invoiceDiscount > 0)
                AddParagraph("خصم الفاتورة: " + invoiceDiscount.ToString("N2") + " ج.م");
            AddParagraph("الإجمالي النهائي: " + invoice.Total.ToString("N2") + " ج.م", 15, true);
            AddParagraph("المدفوع: " + invoice.Paid.ToString("N2") + " ج.م");
            AddParagraph("المتبقي: " + invoice.Remaining.ToString("N2") + " ج.م");
            AddParagraph("الحالة: " + invoice.Status);
        }
        else
        {
            foreach (var property in selected.GetType().GetProperties())
                AddParagraph(property.Name + ": " + (property.GetValue(selected)?.ToString() ?? "-"));
        }
        AddParagraph("شكرًا لتعاملكم معنا", 12, false, TextAlignment.Center);
        void PrintDocument()
        {
            try
            {
                var dialog = new PrintDialog();
                if (dialog.ShowDialog() == true)
                {
                    IDocumentPaginatorSource printable = document;
                    dialog.PrintDocument(printable.DocumentPaginator,
                        isInvoice ? "فاتورة " + invoice!.Number : "معاينة سجل");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("تعذرت الطباعة:\n" + ex.Message,
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
            Width = 780,
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
}
