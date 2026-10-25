
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
                $"حدث خطأ غير متوقع وسيتم إغلاق البرنامج بأمان.\n{Views.ArabicUi.ErrorMessage(args.Exception, "يرجى إعادة تشغيل البرنامج، وإذا تكرر الخطأ فتواصل مع الدعم الفني.")}",
                "سمارت لإدارة الأعمال - خطأ في التشغيل", System.Windows.MessageBoxButton.OK,
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
        catch (Exception)
        {
            System.Windows.MessageBox.Show(
                "تعذر بدء البرنامج أو تهيئة قاعدة البيانات. تحقق من إعدادات الجهاز ومجلد البيانات، ثم حاول مرة أخرى.",
                "سمارت لإدارة الأعمال - تعذر بدء التشغيل", System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            Shutdown(-1);
        }
    }
}
