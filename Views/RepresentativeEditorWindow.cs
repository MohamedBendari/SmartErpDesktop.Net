using System.Windows;
using System.Windows.Controls;
using SmartErpDesktop.Models;
using SmartErpDesktop.Services;

namespace SmartErpDesktop.Views;

public sealed class RepresentativeEditorWindow : Window
{
    private readonly MockErpService service;
    private readonly Representative? existing;
    private readonly TextBox code = new();
    private readonly TextBox name = new();
    private readonly TextBox phone = new();
    private readonly TextBox address = new();
    private readonly TextBox notes = new() { MinHeight = 72, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };

    public RepresentativeEditorWindow(MockErpService service, Representative? representative = null)
    {
        this.service = service;
        existing = representative;
        Title = representative is null ? "إضافة مندوب" : "تعديل بيانات المندوب";
        Width = 500;
        Height = 570;
        MinWidth = 440;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        FlowDirection = FlowDirection.RightToLeft;
        Background = System.Windows.Media.Brushes.White;

        code.Text = representative?.Code ?? "";
        name.Text = representative?.Name ?? "";
        phone.Text = representative?.Phone ?? "";
        address.Text = representative?.Address ?? "";
        notes.Text = representative?.Notes ?? "";

        var root = new StackPanel { Margin = new Thickness(20) };
        root.Children.Add(new TextBlock { Text = Title, FontSize = 22, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 12) });
        AddField(root, "كود المندوب *", code);
        AddField(root, "اسم المندوب *", name);
        AddField(root, "الهاتف", phone);
        AddField(root, "العنوان", address);
        AddField(root, "ملاحظات", notes);
        var save = new Button { Content = "حفظ", Padding = new Thickness(18, 8, 18, 8), Margin = new Thickness(4, 14, 4, 4), HorizontalAlignment = HorizontalAlignment.Left };
        save.Click += Save_Click;
        root.Children.Add(save);
        Content = new ScrollViewer { Content = root };
    }

    private static void AddField(Panel panel, string label, TextBox box)
    {
        panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(4, 6, 4, 2) });
        box.Margin = new Thickness(4);
        box.MinHeight = Math.Max(box.MinHeight, 32);
        panel.Children.Add(box);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            service.SaveRepresentative(existing, code.Text, name.Text, phone.Text, address.Text, notes.Text);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ArabicUi.ErrorMessage(ex, "تعذر حفظ بيانات المندوب. تحقق من البيانات وحاول مرة أخرى."), "بيانات المندوب", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
