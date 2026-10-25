using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using SmartErpDesktop.Models;
using SmartErpDesktop.Services;

namespace SmartErpDesktop.Views;

/// <summary>نافذة إضافة/تعديل صنف مبنية بالكود حتى لا نغيّر هيكل المشروع الحالي.</summary>
public sealed class ProductEditorWindow : Window
{
    private readonly MockErpService service;
    private readonly Product? original;
    private readonly TextBox codeBox = new();
    private readonly TextBox barcodeBox = new();
    private readonly TextBox nameBox = new();
    private readonly TextBox categoryBox = new();
    private readonly TextBox priceBox = new();
    private readonly TextBox costBox = new();
    private readonly TextBox stockBox = new();
    private readonly TextBox minimumBox = new();

    public Product? SavedProduct { get; private set; }

    public ProductEditorWindow(MockErpService service, Product? product = null)
    {
        this.service = service;
        original = product;
        Title = product is null ? "إضافة صنف جديد" : "تعديل بيانات الصنف";
        Width = 520;
        Height = 610;
        MinWidth = 480;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FlowDirection = FlowDirection.RightToLeft;
        ResizeMode = ResizeMode.NoResize;

        var root = new Grid { Margin = new Thickness(18) };
        for (int i = 0; i < 9; i++) root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        AddField(root, 0, "كود الصنف *", codeBox);
        AddField(root, 1, "الباركود *", barcodeBox);
        var barcodeButtons = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        var generateButton = new Button { Content = "توليد باركود", Padding = new Thickness(10, 5, 10, 5) };
        generateButton.Click += (_, _) => barcodeBox.Text = service.GenerateUniqueBarcode();
        barcodeButtons.Children.Add(generateButton);
        Grid.SetRow(barcodeButtons, 2); root.Children.Add(barcodeButtons);
        AddField(root, 3, "اسم الصنف *", nameBox);
        AddField(root, 4, "التصنيف", categoryBox);
        AddField(root, 5, "سعر البيع (ج.م) *", priceBox);
        AddField(root, 6, "التكلفة (ج.م)", costBox);
        AddField(root, 7, "الكمية الحالية", stockBox);
        AddField(root, 8, "حد تنبيه المخزون", minimumBox);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 12, 0, 0) };
        var save = new Button { Content = "حفظ", MinWidth = 100, Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(0, 0, 8, 0) };
        var cancel = new Button { Content = "إلغاء", MinWidth = 90, Padding = new Thickness(14, 8, 14, 8) };
        save.Click += Save_Click;
        cancel.Click += (_, _) => DialogResult = false;
        buttons.Children.Add(save); buttons.Children.Add(cancel);
        Grid.SetRow(buttons, 10); root.Children.Add(buttons);
        Content = new ScrollViewer { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };

        if (product is not null)
        {
            codeBox.Text = product.Code;
            barcodeBox.Text = product.Barcode;
            nameBox.Text = product.Name;
            categoryBox.Text = product.Category;
            priceBox.Text = product.Price.ToString(CultureInfo.InvariantCulture);
            costBox.Text = product.Cost.ToString(CultureInfo.InvariantCulture);
            stockBox.Text = product.Stock.ToString(CultureInfo.InvariantCulture);
            minimumBox.Text = product.MinimumStock.ToString(CultureInfo.InvariantCulture);
            // الرصيد لا يُعدّل من شاشة بيانات الصنف؛ التعديل يكون من الجرد فقط.
            stockBox.IsReadOnly = true;
            stockBox.ToolTip = "لتعديل الكمية استخدم شاشة الجرد والتسويات.";
        }
        else
        {
            stockBox.Text = "0";
            minimumBox.Text = "0";
            priceBox.Text = "0";
            costBox.Text = "0";
        }
    }

    private static void AddField(Grid grid, int row, string label, TextBox box)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
        panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 3) });
        box.Padding = new Thickness(7, 5, 7, 5);
        panel.Children.Add(box);
        Grid.SetRow(panel, row);
        grid.Children.Add(panel);
    }

    private static bool TryNumber(string text, out decimal value) =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value) ||
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        if (!TryNumber(priceBox.Text, out var price) ||
            !TryNumber(costBox.Text, out var cost) ||
            !TryNumber(stockBox.Text, out var stock) ||
            !TryNumber(minimumBox.Text, out var minimum))
        {
            MessageBox.Show("تأكد أن السعر والتكلفة والكميات أرقام صحيحة.", "بيانات غير صحيحة", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var product = new Product
        {
            Code = codeBox.Text.Trim(),
            Barcode = barcodeBox.Text.Trim(),
            Name = nameBox.Text.Trim(),
            Category = categoryBox.Text.Trim(),
            Price = price,
            Cost = cost,
            Stock = original?.Stock ?? stock,
            MinimumStock = minimum
        };
        try
        {
            if (original is null) service.AddProduct(product);
            else service.UpdateProduct(original.Code, product);
            SavedProduct = original ?? product;
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ArabicUi.ErrorMessage(ex, "تعذر حفظ بيانات الصنف. تحقق من البيانات وحاول مرة أخرى."), "تعذر حفظ الصنف", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
