using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using SmartErpDesktop.Models;
using SmartErpDesktop.Services;

namespace SmartErpDesktop.Views;

public sealed class CommissionRuleEditorWindow : Window
{
    private sealed record Choice(string Value, string Label)
    {
        public override string ToString() => Label;
    }

    private readonly MockErpService service;
    private readonly CommissionRule? existing;
    private readonly ComboBox representative = new();
    private readonly ComboBox product = new();
    private readonly ComboBox saleType = new();
    private readonly TextBox description = new();
    private readonly TextBox rate = new();
    private readonly TextBox priority = new();
    private readonly CheckBox active = new() { Content = "قاعدة نشطة", IsChecked = true };

    public CommissionRuleEditorWindow(MockErpService service, CommissionRule? rule = null)
    {
        this.service = service;
        existing = rule;
        Title = rule is null ? "إضافة قاعدة عمولة" : "تعديل قاعدة عمولة";
        Width = 560;
        Height = 690;
        MinWidth = 500;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FlowDirection = FlowDirection.RightToLeft;
        Background = System.Windows.Media.Brushes.White;

        var representatives = service.Representatives.OrderBy(x => x.Name).ToList();
        if (rule is not null && !representatives.Any(x => x.Code.Equals(rule.RepresentativeCode, StringComparison.OrdinalIgnoreCase)))
            representatives.Add(new Representative { Code = rule.RepresentativeCode, Name = "(مندوب غير موجود) " + rule.RepresentativeCode, IsActive = false });
        representative.ItemsSource = representatives;
        representative.DisplayMemberPath = "Name";
        representative.SelectedItem = representatives.FirstOrDefault(x => x.Code.Equals(rule?.RepresentativeCode, StringComparison.OrdinalIgnoreCase));

        var productChoices = new List<Choice> { new("", "كل الأصناف") };
        productChoices.AddRange(service.Products.OrderBy(x => x.Name).Select(x => new Choice(x.Code, $"{x.Code} | {x.Name}")));
        product.ItemsSource = productChoices;
        product.SelectedItem = productChoices.FirstOrDefault(x => x.Value.Equals(rule?.ProductCode ?? "", StringComparison.OrdinalIgnoreCase));

        var saleChoices = new List<Choice> { new("", "كل أنواع البيع") };
        saleChoices.AddRange(service.CommissionSaleTypes.Select(x => new Choice(x, x)));
        saleType.ItemsSource = saleChoices;
        saleType.SelectedItem = saleChoices.FirstOrDefault(x => x.Value.Equals(rule?.SaleType ?? "", StringComparison.OrdinalIgnoreCase));

        description.Text = rule?.Description ?? "";
        rate.Text = rule?.RatePercent.ToString(CultureInfo.CurrentCulture) ?? "0";
        priority.Text = rule?.Priority.ToString(CultureInfo.CurrentCulture) ?? "100";
        active.IsChecked = rule?.IsActive ?? true;

        var root = new StackPanel { Margin = new Thickness(20) };
        root.Children.Add(new TextBlock { Text = Title, FontSize = 22, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 12) });
        AddField(root, "المندوب *", representative);
        AddField(root, "وصف القاعدة *", description);
        AddField(root, "الصنف (اختياري)", product);
        AddField(root, "نوع البيع (اختياري)", saleType);
        AddField(root, "نسبة العمولة % *", rate);
        AddField(root, "الأولوية (الأقل يُختار أولاً داخل النطاق)", priority);
        root.Children.Add(new TextBlock { Text = "الأساس: قيمة البند بعد خصم البند وقبل خصم الفاتورة العام.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4, 8, 4, 4), Foreground = System.Windows.Media.Brushes.DimGray });
        root.Children.Add(active);
        var save = new Button { Content = "حفظ", Padding = new Thickness(18, 8, 18, 8), Margin = new Thickness(4, 14, 4, 4), HorizontalAlignment = HorizontalAlignment.Left };
        save.Click += Save_Click;
        root.Children.Add(save);
        Content = new ScrollViewer { Content = root };
    }

    private static void AddField(Panel panel, string label, Control control)
    {
        panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(4, 6, 4, 2) });
        control.Margin = new Thickness(4);
        control.MinHeight = 32;
        panel.Children.Add(control);
    }

    private static string ChoiceValue(ComboBox box) => (box.SelectedItem as Choice)?.Value ?? "";

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!decimal.TryParse(rate.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsedRate) &&
                !decimal.TryParse(rate.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out parsedRate))
                throw new InvalidOperationException("أدخل نسبة عمولة رقمية صحيحة.");
            if (!int.TryParse(priority.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var parsedPriority) &&
                !int.TryParse(priority.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedPriority))
                throw new InvalidOperationException("أدخل أولوية صحيحة.");
            if (representative.SelectedItem is not Representative selectedRepresentative)
                throw new InvalidOperationException("اختر المندوب.");

            service.SaveCommissionRule(existing, selectedRepresentative.Code, description.Text,
                ChoiceValue(product), ChoiceValue(saleType), parsedRate, parsedPriority, active.IsChecked == true);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ArabicUi.ErrorMessage(ex, "تعذر حفظ قاعدة العمولة. تحقق من البيانات وحاول مرة أخرى."), "بيانات قاعدة العمولة", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
