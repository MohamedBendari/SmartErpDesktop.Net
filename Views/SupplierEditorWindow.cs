using System.Windows;
using System.Windows.Controls;
using SmartErpDesktop.Models;
using SmartErpDesktop.Services;

namespace SmartErpDesktop.Views;

public sealed class SupplierEditorWindow : Window
{
    private readonly MockErpService service;
    private readonly Supplier? existing;
    private readonly TextBox code = new();
    private readonly TextBox name = new();
    private readonly TextBox phone = new();
    private readonly TextBox address = new();
    private readonly TextBox notes = new();

    public SupplierEditorWindow(MockErpService service, Supplier? supplier = null)
    {
        this.service = service; existing = supplier;
        Title = supplier is null ? "إضافة مورد" : "تعديل بيانات المورد"; Width = 480; Height = 440;
        WindowStartupLocation = WindowStartupLocation.CenterOwner; FlowDirection = FlowDirection.RightToLeft;
        code.Text = supplier?.Code ?? ""; name.Text = supplier?.Name ?? ""; phone.Text = supplier?.Phone ?? "";
        address.Text = supplier?.Address ?? ""; notes.Text = supplier?.Notes ?? "";
        var root = new StackPanel { Margin = new Thickness(20) };
        root.Children.Add(new TextBlock { Text = Title, FontSize = 22, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 12) });
        AddField(root, "كود المورد *", code); AddField(root, "اسم المورد *", name); AddField(root, "الهاتف", phone); AddField(root, "العنوان", address); AddField(root, "ملاحظات", notes);
        var save = new Button { Content = "حفظ", Padding = new Thickness(18, 8, 18, 8), Margin = new Thickness(4), HorizontalAlignment = HorizontalAlignment.Left };
        save.Click += (_, _) => { try { service.SaveSupplier(existing, code.Text, name.Text, phone.Text, address.Text, notes.Text); DialogResult = true; } catch (Exception ex) { MessageBox.Show(ArabicUi.ErrorMessage(ex, "تعذر حفظ بيانات المورد. تحقق من البيانات وحاول مرة أخرى."), "بيانات المورد", MessageBoxButton.OK, MessageBoxImage.Warning); } };
        root.Children.Add(save); Content = new ScrollViewer { Content = root };
    }

    private static void AddField(Panel panel, string label, TextBox box) { panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(4, 6, 4, 2) }); box.Margin = new Thickness(4); panel.Children.Add(box); }
}
