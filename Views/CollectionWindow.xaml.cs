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
            var message = $"تم تسجيل الإيصال {payment.ReceiptNumber}\nالرصيد قبل السداد: {payment.BalanceBefore:N2} ج.م\nالرصيد بعد السداد: {payment.BalanceAfter:N2} ج.م";
            MessageBox.Show(message, "نجاح");
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "تعذر تسجيل التحصيل");
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
