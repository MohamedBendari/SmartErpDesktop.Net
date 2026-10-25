using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using SmartErpDesktop.Models;
using SmartErpDesktop.Services;

namespace SmartErpDesktop.Views;

public partial class CollectionWindow : Window
{
    private readonly MockErpService service;

    public CollectionWindow(MockErpService s)
    {
        InitializeComponent();
        service = s;
        CustomerBox.ItemsSource = service.Customers.Where(customer => customer.Balance > 0).ToList();
        if (CustomerBox.Items.Count > 0) CustomerBox.SelectedIndex = 0;
    }

    private void Customer_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (CustomerBox.SelectedItem is Customer customer)
            AmountBox.ToolTip = $"الرصيد الحالي: {customer.Balance:N2} ج.م";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (CustomerBox.SelectedItem is not Customer customer)
        {
            MessageBox.Show("اختر عميلاً لديه رصيد.");
            return;
        }

        if (!decimal.TryParse(AmountBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var amount) &&
            !decimal.TryParse(AmountBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
        {
            MessageBox.Show("أدخل مبلغاً رقمياً صحيحاً.");
            return;
        }

        try
        {
            var method = (MethodBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "نقدي";
            var payment = service.Collect(customer, amount, method, NoteBox.Text);
            var allocations = string.Join(", ", payment.Allocations.Select(x => $"{x.InvoiceNumber}: {x.Amount:N2}"));
            var message = $"\u062A\u0645 \u062A\u0633\u062C\u064A\u0644 \u0627\u0644\u0625\u064A\u0635\u0627\u0644 {payment.ReceiptNumber}\n\u0627\u0644\u0631\u0635\u064A\u062F \u0642\u0628\u0644 \u0627\u0644\u0633\u062F\u0627\u062F: {payment.BalanceBefore:N2} \u062C.\u0645\n\u0627\u0644\u0631\u0635\u064A\u062F \u0628\u0639\u062F \u0627\u0644\u0633\u062F\u0627\u062F: {payment.BalanceAfter:N2} \u062C.\u0645\n\u0627\u0644\u0645\u0648\u0632\u0639 \u0639\u0644\u0649 \u0627\u0644\u0641\u0648\u0627\u062A\u064A\u0631: {allocations}\n\u063A\u064A\u0631 \u0627\u0644\u0645\u0648\u0632\u0639: {payment.UnallocatedAmount:N2} \u062C.\u0645";
            MessageBox.Show(message, "نجاح");
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ArabicUi.ErrorMessage(ex, "تعذر تسجيل التحصيل. تحقق من البيانات وحاول مرة أخرى."), "تعذر تسجيل التحصيل");
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
