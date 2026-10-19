using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using SmartErpDesktop.Models;

namespace SmartErpDesktop.Views;

/// <summary>معاينة وطباعة ملصق Code 39 باستخدام عناصر WPF فقط.</summary>
public sealed class BarcodeLabelWindow : Window
{
    private readonly Product product;
    private readonly Border labelBorder;
    private static readonly IReadOnlyDictionary<char, string> Patterns = new Dictionary<char, string>
    {
        ['0'] = "nnnwwnwnn",
        ['1'] = "wnnwnnnnw",
        ['2'] = "nnwwnnnnw",
        ['3'] = "wnwwnnnnn",
        ['4'] = "nnnwwnnnw",
        ['5'] = "wnnwwnnnn",
        ['6'] = "nnwwwnnnn",
        ['7'] = "nnnwnnwnw",
        ['8'] = "wnnwnnwnn",
        ['9'] = "nnwwnnwnn",
        ['A'] = "wnnnnwnnw",
        ['B'] = "nnwnnwnnw",
        ['C'] = "wnwnnwnnn",
        ['D'] = "nnnnwwnnw",
        ['E'] = "wnnnwwnnn",
        ['F'] = "nnwnwwnnn",
        ['G'] = "nnnnnwwnw",
        ['H'] = "wnnnnwwnn",
        ['I'] = "nnwnnwwnn",
        ['J'] = "nnnnwwwnn",
        ['K'] = "wnnnnnnww",
        ['L'] = "nnwnnnnww",
        ['M'] = "wnwnnnnwn",
        ['N'] = "nnnnwnnww",
        ['O'] = "wnnnwnnwn",
        ['P'] = "nnwnwnnwn",
        ['Q'] = "nnnnnnwww",
        ['R'] = "wnnnnnwwn",
        ['S'] = "nnwnnnwwn",
        ['T'] = "nnnnwnwwn",
        ['U'] = "wwnnnnnnw",
        ['V'] = "nwwnnnnnw",
        ['W'] = "wwwnnnnnn",
        ['X'] = "nwnnwnnnw",
        ['Y'] = "wwnnwnnnn",
        ['Z'] = "nwwnwnnnn",
        ['-'] = "nwnnnnwnw",
        ['.'] = "wwnnnnwnn",
        [' '] = "nwwnnnwnn",
        ['$'] = "nwnwnwnnn",
        ['/'] = "nwnwnnnwn",
        ['+'] = "nwnnnwnwn",
        ['%'] = "nnnwnwnwn",
        ['*'] = "nwnnwnwnn"
    };

    public BarcodeLabelWindow(Product product)
    {
        this.product = product ?? throw new ArgumentNullException(nameof(product));
        Title = "معاينة وطباعة باركود"; Width = 500; Height = 340; MinWidth = 460;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FlowDirection = FlowDirection.RightToLeft; Background = Brushes.White;
        var root = new DockPanel { Margin = new Thickness(16) };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 12, 0, 0) };
        var print = new Button { Content = "طباعة الملصق", Padding = new Thickness(16, 8, 16, 8), Margin = new Thickness(0, 0, 8, 0) };
        var close = new Button { Content = "إغلاق", Padding = new Thickness(16, 8, 16, 8) };
        print.Click += Print_Click; close.Click += (_, _) => Close(); buttons.Children.Add(print); buttons.Children.Add(close);
        DockPanel.SetDock(buttons, Dock.Bottom); root.Children.Add(buttons);
        labelBorder = new Border { BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(1), Padding = new Thickness(12), Background = Brushes.White, Width = 420, Height = 205, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var label = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        label.Children.Add(new TextBlock { Text = product.Name, FontSize = 16, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = 380, Margin = new Thickness(0, 0, 0, 4) });
        label.Children.Add(BuildBarcode(product.Barcode));
        label.Children.Add(new TextBlock { Text = product.Barcode, FontFamily = new FontFamily("Consolas"), FontSize = 13, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 4, 0, 2) });
        label.Children.Add(new TextBlock { Text = $"{product.Code}  |  {product.Price:N2} ج.م", FontSize = 11, TextAlignment = TextAlignment.Center });
        labelBorder.Child = label; root.Children.Add(labelBorder); Content = root;
    }

    private static Canvas BuildBarcode(string value)
    {
        var normalized = (value ?? string.Empty).Trim().ToUpperInvariant();
        if (normalized.Length == 0) throw new ArgumentException("رقم الباركود فارغ.", nameof(value));
        foreach (char c in normalized)
            if (!Patterns.ContainsKey(c) || c == '*')
                throw new ArgumentException($"الرمز '{c}' غير مدعوم في Code 39. استخدم A-Z والأرقام والرموز - . $ / + % والمسافة.", nameof(value));
        string encoded = "*" + normalized + "*";
        const double narrow = 1.2, wide = narrow * 3, gap = narrow, barHeight = 92;
        double x = 12;
        var canvas = new Canvas { Width = encoded.Sum(ch => Patterns[ch].Sum(w => w == 'w' ? wide : narrow) + gap) + 12, Height = barHeight, HorizontalAlignment = HorizontalAlignment.Center };
        foreach (char ch in encoded)
        {
            string pattern = Patterns[ch];
            for (int i = 0; i < pattern.Length; i++)
            {
                double width = pattern[i] == 'w' ? wide : narrow;
                if (i % 2 == 0)
                {
                    var bar = new Rectangle { Width = width, Height = barHeight, Fill = Brushes.Black, SnapsToDevicePixels = true };
                    Canvas.SetLeft(bar, x); Canvas.SetTop(bar, 0); canvas.Children.Add(bar);
                }
                x += width;
            }
            x += gap;
        }
        return canvas;
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            labelBorder.Measure(new Size(labelBorder.Width, labelBorder.Height));
            labelBorder.Arrange(new Rect(0, 0, labelBorder.Width, labelBorder.Height));
            labelBorder.UpdateLayout();
            var dialog = new PrintDialog();
            if (dialog.ShowDialog() == true) dialog.PrintVisual(labelBorder, $"Barcode label - {product.Code}");
        }
        catch (Exception ex)
        {
            MessageBox.Show("تعذرت طباعة ملصق الباركود:\n" + ex.Message, "خطأ في الطباعة", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
