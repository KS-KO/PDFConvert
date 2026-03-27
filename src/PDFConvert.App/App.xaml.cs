using System.Threading;
using System.Windows;
using PDFConvert.App.Bootstrapping;

namespace PDFConvert.App;

public partial class App : System.Windows.Application
{
    private const string UniqueMutexName = "PDFConvert-8D7F91C4-08A2-4B6A-B6C2-D8D8E9E9C9F9";
    private Mutex? _mutex;
    private Bootstrapper? _bootstrapper;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, UniqueMutexName, out bool createdNew);

        if (!createdNew)
        {
            System.Windows.MessageBox.Show("프로그램이 이미 실행 중입니다.", "PDF Converter", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        base.OnStartup(e);

        _bootstrapper = new Bootstrapper();
        var mainWindow = _bootstrapper.CreateMainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        _mutex = null;

        base.OnExit(e);
    }
}
