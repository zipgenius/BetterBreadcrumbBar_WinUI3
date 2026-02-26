using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

namespace BetterBreadcrumbBar.Demo;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        // Inizializzazione obbligatoria per deployment non pacchettizzato (unpackaged)
        // Richiede che il Windows App SDK Runtime sia installato sul sistema
        Microsoft.Windows.ApplicationModel.DynamicDependency.Bootstrap.Initialize(
            0x00010008); // versione minima: 1.8.x

        this.InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
