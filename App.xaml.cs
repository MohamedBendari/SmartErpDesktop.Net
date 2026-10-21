
using SmartErpDesktop.Services;

namespace SmartErpDesktop;

public partial class App : System.Windows.Application
{
    public CompanySettingsService CompanySettings { get; private set; } = null!;

    protected override void OnStartup(
        System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, args) =>
        {
            System.Windows.MessageBox.Show(
                $"حدث خطأ غير متوقع وسيتم إغلاق البرنامج بأمان.\n{args.Exception.Message}",
                "SMART ERP - خطأ في التشغيل", System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            args.Handled = true;
            Shutdown(-1);
        };

        try
        {
            CompanySettings = new CompanySettingsService();
            var mainWindow = new Views.MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"تعذر بدء SMART ERP أو تهيئة قاعدة البيانات. تحقق من صلاحية الكتابة في مجلد بيانات المستخدم.\n\nالتفاصيل: {ex.Message}",
                "SMART ERP - تعذر بدء التشغيل", System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            Shutdown(-1);
        }
    }
}
