
using SmartErpDesktop.Models;
using SmartErpDesktop.Services;
using System;
using System.Windows;
using System.Windows.Controls;

namespace SmartErpDesktop.Views;

public partial class SettingsView : UserControl
{
    private readonly CompanySettingsService service;

    public SettingsView(CompanySettingsService service)
    {
        InitializeComponent();

        this.service = service;

        LoadSettings();
        ShowSection(GeneralSection);
    }

    private void GeneralSection_Click(object sender, RoutedEventArgs e)
        => ShowSection(GeneralSection);

    private void PrintingSection_Click(object sender, RoutedEventArgs e)
        => ShowSection(PrintingSection);

    private void AppearanceSection_Click(object sender, RoutedEventArgs e)
        => ShowSection(AppearanceSection);

    private void AboutSection_Click(object sender, RoutedEventArgs e)
        => ShowSection(AboutSection);

    private void ShowSection(StackPanel selected)
    {
        GeneralSection.Visibility = Visibility.Collapsed;
        PrintingSection.Visibility = Visibility.Collapsed;
        AppearanceSection.Visibility = Visibility.Collapsed;
        AboutSection.Visibility = Visibility.Collapsed;

        selected.Visibility = Visibility.Visible;
    }

    private void LoadSettings()
    {
        try
        {
            ApplySettings(service.GetSettings());
            StatusText.Text = "تم تحميل الإعدادات المحفوظة.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "تعذر تحميل الإعدادات.";

            MessageBox.Show(
                "تعذر تحميل الإعدادات:\n" + ex.Message,
                "خطأ",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ApplySettings(CompanySettings settings)
    {
        StoreNameBox.Text = settings.StoreName;
        PhoneBox.Text = settings.Phone;
        AddressBox.Text = settings.Address;
        TaxNumberBox.Text = settings.TaxNumber;
        PrinterNameBox.Text = settings.PrinterName;

        SelectCombo(PaperSizeBox, settings.PaperSize);
        SelectCombo(ThemeBox, settings.Theme);

        ShowStoreInfoCheck.IsChecked = settings.ShowStoreInfo;
        ShowPrintPreviewCheck.IsChecked = settings.ShowPrintPreview;
    }

    private static void SelectCombo(ComboBox combo, string value)
    {
        foreach (ComboBoxItem item in combo.Items)
        {
            if (string.Equals(
                item.Content?.ToString(),
                value,
                StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedItem = item;
                return;
            }
        }

        if (combo.Items.Count > 0)
            combo.SelectedIndex = 0;
    }

    private CompanySettings ReadForm()
    {
        return new CompanySettings
        {
            Id = 1,
            StoreName = StoreNameBox.Text.Trim(),
            Phone = PhoneBox.Text.Trim(),
            Address = AddressBox.Text.Trim(),
            TaxNumber = TaxNumberBox.Text.Trim(),
            PrinterName = PrinterNameBox.Text.Trim(),

            PaperSize =
                (PaperSizeBox.SelectedItem as ComboBoxItem)?
                .Content?.ToString() ?? "حراري 80 مم",

            Theme =
                (ThemeBox.SelectedItem as ComboBoxItem)?
                .Content?.ToString() ?? "فاتح",

            ShowStoreInfo = ShowStoreInfoCheck.IsChecked == true,
            ShowPrintPreview = ShowPrintPreviewCheck.IsChecked == true
        };
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(StoreNameBox.Text))
        {
            ShowValidation("اسم المنشأة مطلوب.", StoreNameBox);
            return;
        }

        try
        {
            service.SaveSettings(ReadForm());

            StatusText.Text =
                $"تم الحفظ بنجاح — {DateTime.Now:HH:mm:ss}";

            MessageBox.Show(
                "تم حفظ بيانات المنشأة والإعدادات في قاعدة البيانات المحلية.",
                "تم الحفظ",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusText.Text = "فشل حفظ الإعدادات.";

            MessageBox.Show(
                "تعذر حفظ الإعدادات:\n" + ex.Message,
                "خطأ في الحفظ",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private static void ShowValidation(string message, Control control)
    {
        MessageBox.Show(
            message,
            "مراجعة البيانات",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);

        control.Focus();
    }
}
