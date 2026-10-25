using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SmartErpDesktop.Models;
using SmartErpDesktop.Services;

namespace SmartErpDesktop.Views;

public partial class RepresentativesView : UserControl
{
    public sealed record RepresentativeRow(string Code, string Name, string Phone, string Status, Representative Representative);

    private readonly MockErpService service;
    private readonly Action<Representative> openDetails;

    public RepresentativesView(MockErpService service, Action<Representative> openDetails)
    {
        InitializeComponent();
        this.service = service;
        this.openDetails = openDetails;
        RepresentativesGrid.SelectionChanged += (_, _) => UpdateToggleButton();
        LoadData();
    }

    private Representative? Selected => (RepresentativesGrid.SelectedItem as RepresentativeRow)?.Representative;

    private void LoadData()
    {
        var query = FilterBox.Text.Trim();
        var matches = service.Representatives
            .Where(r => query.Length == 0 || r.Code.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        r.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        r.Phone.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(r => new RepresentativeRow(r.Code, r.Name, r.Phone, r.IsActive ? "نشط" : "غير نشط", r))
            .ToList();
        RepresentativesGrid.ItemsSource = matches;
        var activeCount = service.Representatives.Count(r => r.IsActive);
        SummaryText.Text = $"المندوبون: {service.Representatives.Count} | نشط: {activeCount} | غير نشط: {service.Representatives.Count - activeCount}";
        UpdateToggleButton();
    }

    private void UpdateToggleButton()
    {
        ToggleStatusButton.IsEnabled = Selected is not null;
        ToggleStatusButton.Content = Selected?.IsActive == true ? "إيقاف المندوب" : "تفعيل المندوب";
    }

    private void Search_Click(object sender, RoutedEventArgs e) => LoadData();
    private void FilterBox_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) LoadData(); }
    private void Refresh_Click(object sender, RoutedEventArgs e) { FilterBox.Clear(); LoadData(); }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var window = new RepresentativeEditorWindow(service) { Owner = Window.GetWindow(this) };
        if (window.ShowDialog() == true) LoadData();
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        var representative = Selected;
        if (representative is null) { MessageBox.Show("حدد مندوباً من القائمة أولاً."); return; }
        var window = new RepresentativeEditorWindow(service, representative) { Owner = Window.GetWindow(this) };
        if (window.ShowDialog() == true) LoadData();
    }

    private void Details_Click(object sender, RoutedEventArgs e)
    {
        var representative = Selected;
        if (representative is null) { MessageBox.Show("حدد مندوباً لعرض تفاصيله."); return; }
        openDetails(representative);
    }

    private void ToggleStatus_Click(object sender, RoutedEventArgs e)
    {
        var representative = Selected;
        if (representative is null) return;
        var action = representative.IsActive ? "إيقاف" : "تفعيل";
        if (MessageBox.Show($"هل تريد {action} المندوب {representative.Name}؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        try
        {
            service.SetRepresentativeActive(representative, !representative.IsActive);
            LoadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ArabicUi.ErrorMessage(ex, "تعذر تحديث حالة المندوب."), "تعذر تحديث حالة المندوب", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
