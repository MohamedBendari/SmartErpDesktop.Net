using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using SmartErpDesktop.Models;
using SmartErpDesktop.Services;

namespace SmartErpDesktop.Views;

/// <summary>تسجيل تحصيل مرتبط بقسط واحد محدد.</summary>
public sealed class InstallmentCollectionWindow : Window
{
    private readonly MockErpService service;
    private readonly Installment installment;
    private readonly TextBox amountBox = new();
    private readonly ComboBox methodBox = new();
    private readonly TextBox noteBox = new();

    public InstallmentCollectionWindow(MockErpService service, Installment installment)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
        this.installment = installment ?? throw new ArgumentNullException(nameof(installment));
        Title = "تسجيل تحصيل قسط";
        Width = 520;
        Height = 500;
        MinWidth = 460;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FlowDirection = FlowDirection.RightToLeft;
        Background = System.Windows.Media.Brushes.White;
        ResizeMode = ResizeMode.NoResize;

        var root = new Grid { Margin = new Thickness(18) };
        for (int i = 0; i < 10; i++) root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var title = new TextBlock { Text = "تسجيل تحصيل على قسط", FontSize = 22, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 12) };
        root.Children.Add(title);
        AddInfo(root, 1, "العميل", installment.CustomerName);
        AddInfo(root, 2, "رقم الفاتورة / القسط", $"{installment.InvoiceNumber} / {installment.Sequence}");
        AddInfo(root, 3, "الاستحقاق", installment.DueDate.ToString("yyyy/MM/dd"));
        AddInfo(root, 4, "قيمة القسط", $"{installment.Amount:N2} ج.م");
        AddInfo(root, 5, "المدفوع / المتبقي", $"{installment.PaidAmount:N2} / {installment.Remaining:N2} ج.م");

        var amountPanel = new StackPanel { Margin = new Thickness(0, 8, 0, 4) };
        amountPanel.Children.Add(new TextBlock { Text = "مبلغ التحصيل (ج.م)" });
        amountBox.Text = installment.Remaining.ToString("0.00", CultureInfo.InvariantCulture);
        amountPanel.Children.Add(amountBox);
        Grid.SetRow(amountPanel, 6); root.Children.Add(amountPanel);

        var methodPanel = new StackPanel { Margin = new Thickness(0, 4, 0, 4) };
        methodPanel.Children.Add(new TextBlock { Text = "طريقة الدفع" });
        methodBox.Items.Add("نقدي"); methodBox.Items.Add("تحويل"); methodBox.Items.Add("بطاقة"); methodBox.SelectedIndex = 0;
        methodPanel.Children.Add(methodBox);
        Grid.SetRow(methodPanel, 7); root.Children.Add(methodPanel);

        var notePanel = new StackPanel { Margin = new Thickness(0, 4, 0, 4) };
        notePanel.Children.Add(new TextBlock { Text = "ملاحظات (اختياري)" });
        noteBox.MinHeight = 48; noteBox.AcceptsReturn = true; noteBox.TextWrapping = TextWrapping.Wrap;
        notePanel.Children.Add(noteBox);
        Grid.SetRow(notePanel, 8); root.Children.Add(notePanel);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 12, 0, 0) };
        var save = new Button { Content = "حفظ التحصيل", Padding = new Thickness(16, 8, 16, 8) };
        save.Click += Save_Click;
        var cancel = new Button { Content = "إلغاء", Background = System.Windows.Media.Brushes.SlateGray, Padding = new Thickness(16, 8, 16, 8) };
        cancel.Click += (_, _) => DialogResult = false;
        buttons.Children.Add(save); buttons.Children.Add(cancel);
        Grid.SetRow(buttons, 9); root.Children.Add(buttons);
        Content = root;
    }

    private static void AddInfo(Grid root, int row, string label, string value)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 3) };
        panel.Children.Add(new TextBlock { Text = label + ": ", FontWeight = FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text = value });
        Grid.SetRow(panel, row); root.Children.Add(panel);
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(amountBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var amount) &&
            !decimal.TryParse(amountBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
        {
            MessageBox.Show("أدخل مبلغاً رقمياً صحيحاً.", "مبلغ التحصيل", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try
        {
            var method = methodBox.SelectedItem?.ToString() ?? "نقدي";
            var payment = service.CollectForInstallment(installment, amount, method, noteBox.Text);
            var invoiceInstallments = service.GetInstallments(installment.InvoiceNumber);
            var openInstallments = invoiceInstallments.Where(x => x.Remaining > 0).ToList();
            var totalRemaining = openInstallments.Sum(x => x.Remaining);
            MessageBox.Show(
                $"تم تسجيل الإيصال {payment.ReceiptNumber}\nالفاتورة: {installment.InvoiceNumber}\nالقسط: {installment.Sequence}\nالمحصل: {payment.Amount:N2} ج.م\nالمتبقي على هذا القسط: {installment.Remaining:N2} ج.م\nحالة القسط: {installment.Status}\n\nعدد الأقساط المتبقية على الفاتورة: {openInstallments.Count}\nإجمالي المبلغ المتبقي على الفاتورة: {totalRemaining:N2} ج.م",
                "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "تعذر تسجيل التحصيل", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
