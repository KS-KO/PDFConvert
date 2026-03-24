using PDFConvert.App.Bootstrapping;

namespace PDFConvert.App;

public partial class App : System.Windows.Application
{
    private Bootstrapper? _bootstrapper;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        _bootstrapper = new Bootstrapper();
        var mainWindow = _bootstrapper.CreateMainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();
    }
}
