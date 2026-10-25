
using SmartErpDesktop.Models;
using SmartErpDesktop.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.IO;

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
                ArabicUi.ErrorMessage(ex, "تعذر تحميل الإعدادات من قاعدة البيانات."),
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
        ShowRepresentativeCustomerInvoiceCheck.IsChecked = settings.ShowRepresentativeOnCustomerInvoice;
        ShowRepresentativeInternalInvoiceCheck.IsChecked = settings.ShowRepresentativeOnInternalInvoice;
    }

    private static void SelectCombo(ComboBox combo, string value)
    {
        foreach (ComboBoxItem item in combo.Items)
        {
            if (string.Equals(
                item.Tag?.ToString() ?? item.Content?.ToString(),
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
                .Tag?.ToString() ?? (PaperSizeBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "حراري 80 مم",

            Theme =
                (ThemeBox.SelectedItem as ComboBoxItem)?
                .Content?.ToString() ?? "فاتح",

            ShowStoreInfo = ShowStoreInfoCheck.IsChecked == true,
            ShowPrintPreview = ShowPrintPreviewCheck.IsChecked == true,
            ShowRepresentativeOnCustomerInvoice = ShowRepresentativeCustomerInvoiceCheck.IsChecked == true,
            ShowRepresentativeOnInternalInvoice = ShowRepresentativeInternalInvoiceCheck.IsChecked == true
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
                ArabicUi.ErrorMessage(ex, "تعذر حفظ الإعدادات. حاول مرة أخرى."),
                "خطأ في الحفظ",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }


    private void CreateBackup_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "حفظ نسخة احتياطية من سمارت لإدارة الأعمال",
            Filter = "نسخ قاعدة البيانات (*.db)|*.db|كل الملفات (*.*)|*.*",
            FileName = $"نسخة-احتياطية-{DateTime.Now:yyyyMMdd-HHmmss}.db",
            AddExtension = true,
            DefaultExt = ".db"
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            new BackupService().CreateBackup(dialog.FileName);
            StatusText.Text = "تم إنشاء النسخة الاحتياطية بنجاح.";
            MessageBox.Show("تم حفظ نسخة احتياطية كاملة من قاعدة بيانات البرنامج في:\n" + dialog.FileName,
                "نسخة احتياطية", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ArabicUi.ErrorMessage(ex, "تعذر إنشاء النسخة الاحتياطية. تحقق من صلاحيات المجلد وحاول مرة أخرى."), "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RestoreBackup_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "اختيار نسخة احتياطية لاستعادتها",
            Filter = "نسخ قاعدة البيانات (*.db)|*.db|كل الملفات (*.*)|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog() != true) return;
        var confirm = MessageBox.Show(
            "سيتم استبدال بيانات البرنامج الحالية بالبيانات الموجودة في النسخة المختارة. يُفضّل إنشاء نسخة احتياطية حالية أولًا. هل تريد المتابعة؟",
            "تأكيد استعادة النسخة", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;
        try
        {
            // The application should be restarted after restore so in-memory lists reload from SQLite.
            new BackupService().RestoreBackup(dialog.FileName);
            StatusText.Text = "تمت الاستعادة. أغلق البرنامج وافتحه مرة أخرى.";
            MessageBox.Show("تمت استعادة قاعدة البيانات. أغلق البرنامج وافتحه مرة أخرى حتى تُحمّل البيانات المستعادة.",
                "تمت الاستعادة", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ArabicUi.ErrorMessage(ex, "تعذر استعادة النسخة الاحتياطية. تحقق من الملف وحاول مرة أخرى."), "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
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
