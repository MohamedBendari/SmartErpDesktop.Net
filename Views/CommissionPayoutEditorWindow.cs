using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using SmartErpDesktop.Services;

namespace SmartErpDesktop.Views;

public sealed class CommissionPayoutEditorWindow : Window
{
    private readonly MockErpService service;
    private readonly string representativeCode;
    private readonly TextBox amount = new();
    private readonly TextBox notes = new() { AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 70 };
    private readonly ComboBox method = new();

    public CommissionPayoutEditorWindow(MockErpService service, string representativeCode)
    {
        this.service = service;
        this.representativeCode = representativeCode;
        Title = "تسجيل دفعة عمولة للمندوب";
        Width = 460;
        Height = 430;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FlowDirection = FlowDirection.RightToLeft;
        Background = System.Windows.Media.Brushes.White;
        method.ItemsSource = new[] { "نقدي", "تحويل", "بطاقة" };
        method.SelectedIndex = 0;
        var root = new StackPanel { Margin = new Thickness(20) };
        root.Children.Add(new TextBlock { Text = Title, FontSize = 20, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) });
        root.Children.Add(new TextBlock { Text = $"المتاح للصرف: {service.GetOutstandingCommission(representativeCode):N2} ج.م", FontSize = 16, Margin = new Thickness(4, 4, 4, 8) });
        AddField(root, "المبلغ *", amount);
        AddField(root, "طريقة الدفع *", method);
        AddField(root, "ملاحظات", notes);
        var save = new Button { Content = "حفظ وإصدار إيصال", HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(4, 14, 4, 4) };
        save.Click += Save_Click;
        root.Children.Add(save);
        Content = root;
    }

    private static void AddField(Panel panel, string label, Control control)
    {
        panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(4, 5, 4, 2) });
        control.Margin = new Thickness(4);
        control.MinHeight = Math.Max(32, control.MinHeight);
        panel.Children.Add(control);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(amount.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed) &&
            !decimal.TryParse(amount.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
        {
            MessageBox.Show("أدخل مبلغاً رقمياً صحيحاً.", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try
        {
            var payout = service.RecordRepresentativePayout(representativeCode, parsed, method.SelectedItem?.ToString() ?? "", notes.Text);
            MessageBox.Show($"تم تسجيل الإيصال {payout.ReceiptNumber}", Title, MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ArabicUi.ErrorMessage(ex, "تعذر تسجيل صرف العمولة. تحقق من البيانات وحاول مرة أخرى."), Title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
