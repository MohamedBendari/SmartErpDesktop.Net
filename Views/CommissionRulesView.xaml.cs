using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SmartErpDesktop.Models;
using SmartErpDesktop.Services;

namespace SmartErpDesktop.Views;

public partial class CommissionRulesView : UserControl
{
    public sealed record RuleRow(string Representative, string Description, string Scope, string Rate, string Basis, int Priority, string Status, CommissionRule Rule);

    private readonly MockErpService service;

    public CommissionRulesView(MockErpService service)
    {
        InitializeComponent();
        this.service = service;
        RulesGrid.SelectionChanged += (_, _) => UpdateToggleButton();
        LoadData();
    }

    private CommissionRule? Selected => (RulesGrid.SelectedItem as RuleRow)?.Rule;

    private void LoadData()
    {
        var query = SearchBox.Text.Trim();
        var status = (StatusFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
        var rows = service.CommissionRules
            .Where(r => status == "نشطة" ? r.IsActive : status == "غير نشطة" ? !r.IsActive : true)
            .Where(r => query.Length == 0 || r.RepresentativeCode.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        r.Description.Contains(query, StringComparison.OrdinalIgnoreCase) || r.ProductCode.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        r.SaleType.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r.RepresentativeCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => ScopeRank(r))
            .ThenBy(r => r.Priority)
            .Select(r => new RuleRow(
                service.Representatives.FirstOrDefault(x => x.Code.Equals(r.RepresentativeCode, StringComparison.OrdinalIgnoreCase))?.Name ?? r.RepresentativeCode,
                r.Description,
                FormatScope(r),
                r.RatePercent.ToString("N2"),
                "قيمة البند بعد خصم البند وقبل خصم الفاتورة العام",
                r.Priority,
                r.IsActive ? "نشطة" : "غير نشطة",
                r))
            .ToList();
        RulesGrid.ItemsSource = rows;
        SummaryText.Text = $"القواعد: {service.CommissionRules.Count} | نشطة: {service.CommissionRules.Count(x => x.IsActive)} | غير نشطة: {service.CommissionRules.Count(x => !x.IsActive)}";
        UpdateToggleButton();
    }

    private static int ScopeRank(CommissionRule rule) => rule.ProductCode.Length > 0 && rule.SaleType.Length > 0 ? 0 : rule.ProductCode.Length > 0 ? 1 : rule.SaleType.Length > 0 ? 2 : 3;
    private static string FormatScope(CommissionRule rule) => string.Join(" — ", new[]
    {
        rule.ProductCode.Length > 0 ? "الصنف " + rule.ProductCode : "كل الأصناف",
        rule.SaleType.Length > 0 ? "نوع البيع " + rule.SaleType : "كل أنواع البيع"
    });

    private void UpdateToggleButton()
    {
        ToggleButton.IsEnabled = Selected is not null;
        ToggleButton.Content = Selected?.IsActive == true ? "تعطيل القاعدة" : "تفعيل القاعدة";
    }

    private void Search_Click(object sender, RoutedEventArgs e) => LoadData();
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) { if (IsInitialized) LoadData(); }
    private void SearchBox_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) LoadData(); }
    private void Refresh_Click(object sender, RoutedEventArgs e) { SearchBox.Clear(); StatusFilter.SelectedIndex = 0; LoadData(); }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var window = new CommissionRuleEditorWindow(service) { Owner = Window.GetWindow(this) };
        if (window.ShowDialog() == true) LoadData();
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is not { } rule) { MessageBox.Show("حدد قاعدة من القائمة أولاً."); return; }
        var window = new CommissionRuleEditorWindow(service, rule) { Owner = Window.GetWindow(this) };
        if (window.ShowDialog() == true) LoadData();
    }

    private void Toggle_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is not { } rule) return;
        try
        {
            service.SetCommissionRuleActive(rule, !rule.IsActive);
            LoadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ArabicUi.ErrorMessage(ex, "تعذر تحديث حالة قاعدة العمولة."), "تعذر تحديث حالة القاعدة", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
