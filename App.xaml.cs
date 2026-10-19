
using SmartErpDesktop.Services;

namespace SmartErpDesktop;

public partial class App : System.Windows.Application
{
    public CompanySettingsService CompanySettings { get; private set; } = null!;

    protected override void OnStartup(
        System.Windows.StartupEventArgs e)
    {
        CompanySettings = new CompanySettingsService();

        base.OnStartup(e);
    }
}
