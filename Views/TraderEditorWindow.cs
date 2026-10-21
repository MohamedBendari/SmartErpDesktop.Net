using System.Windows;
using System.Windows.Controls;
using SmartErpDesktop.Models;
using SmartErpDesktop.Services;

namespace SmartErpDesktop.Views;

public sealed class TraderEditorWindow : Window
{
    private readonly MockErpService service;
    private readonly Customer? trader;
    private readonly TextBox code = new(), name = new(), phone = new(), address = new();
    public TraderEditorWindow(MockErpService service, Customer? trader = null)
    {
        this.service = service; this.trader = trader;
        Title = trader is null ? "إضافة تاجر" : "تعديل بيانات تاجر"; Width = 440; Height = 390;
        WindowStartupLocation = WindowStartupLocation.CenterOwner; FlowDirection = FlowDirection.RightToLeft;
        var panel = new StackPanel { Margin = new Thickness(18) };
        Add("الكود", code); Add("اسم التاجر", name); Add("الهاتف", phone); Add("العنوان", address);
        code.Text = trader?.Code ?? ""; name.Text = trader?.Name ?? ""; phone.Text = trader?.Phone ?? ""; address.Text = trader?.Address ?? "";
        var save = new Button { Content = "حفظ", Margin = new Thickness(0, 12, 0, 0), Padding = new Thickness(12) };
        save.Click += Save_Click; panel.Children.Add(save); Content = panel;
        void Add(string label, TextBox box) { panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 6, 0, 2) }); panel.Children.Add(box); }
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try { service.SaveTrader(trader, code.Text, name.Text, phone.Text, address.Text); DialogResult = true; }
        catch (Exception ex) { MessageBox.Show(ex.Message, "تعذر حفظ بيانات التاجر", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }
}
