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
        LoadData();
    }

    private void LoadData()
    {
        object data = module switch
        {
            "Invoices" => service.Invoices.Select(i => new { رقم_الفاتورة = i.Number, التاريخ = i.Date.ToString("yyyy/MM/dd HH:mm"), العميل = i.CustomerName, نوع_البيع = i.Type, الإجمالي = i.Total, المدفوع = i.Paid, المتبقي = i.Remaining, الحالة = i.Status }).ToList(),
            "Customers" => service.Customers.Select(c => new { الكود = c.Code, الاسم = c.Name, الهاتف = c.Phone, العنوان = c.Address, النوع = c.Type, الرصيد = c.Balance }).ToList(),
            "Collections" => service.Payments.Select(p => new { رقم_الإيصال = p.ReceiptNumber, التاريخ = p.Date.ToString("yyyy/MM/dd HH:mm"), العميل = p.CustomerName, المبلغ = p.Amount, الطريقة = p.Method, الرصيد_قبل = p.BalanceBefore, الرصيد_بعد = p.BalanceAfter }).ToList(),
            "Products" => service.Products.Select(p => new { الكود = p.Code, الباركود = p.Barcode, الصنف = p.Name, التصنيف = p.Category, سعر_البيع = p.Price, التكلفة = p.Cost, المتاح = p.Stock, حد_التنبيه = p.MinimumStock, الحالة = p.Stock <= p.MinimumStock ? "مخزون منخفض" : "متاح" }).ToList(),
            "Purchases" => service.Purchases.Select(p => new { رقم_الشراء = p.Number, التاريخ = p.Date.ToString("yyyy/MM/dd"), المورد = p.Supplier, عدد_السطور = p.Lines, الإجمالي = p.Total, الحالة = p.Status }).ToList(),
            "Stock" => service.Movements.Select(m => new { التاريخ = m.Date.ToString("yyyy/MM/dd HH:mm"), الكود = m.ProductCode, الصنف = m.ProductName, نوع_الحركة = m.Type, الكمية = m.Quantity, السبب = m.Reason, المستخدم = m.User }).ToList(),
            _ => new List<object>()
        };
        source = (IEnumerable)data;
        DataGridMain.ItemsSource = source;
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
            var window = new CollectionWindow(service);
            if (window.ShowDialog() == true) LoadData();
        }
        else if (module == "Products")
        {
            if (DataGridMain.SelectedItem is null)
            {
                MessageBox.Show("حدد صنفاً أولاً.");
                return;
            }

            var code = DataGridMain.SelectedItem.GetType().GetProperty("الكود")?.GetValue(DataGridMain.SelectedItem)?.ToString();
            var product = service.Products.FirstOrDefault(p => p.Code == code);
            if (product is null) return;

            var actual = Prompt("أدخل الكمية الفعلية بعد الجرد:", product.Stock.ToString("N2", CultureInfo.CurrentCulture));
            if (!decimal.TryParse(actual, NumberStyles.Number, CultureInfo.CurrentCulture, out var quantity) &&
                !decimal.TryParse(actual, NumberStyles.Number, CultureInfo.InvariantCulture, out quantity))
                return;

            var reason = Prompt("سبب التسوية:", "جرد فعلي");
            try
            {
                service.AdjustStock(product, quantity, reason ?? "جرد فعلي");
                LoadData();
                MessageBox.Show("تم تحديث الكمية وإضافة سجل حركة.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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

        // Company details loaded from the saved SQLite settings.
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
