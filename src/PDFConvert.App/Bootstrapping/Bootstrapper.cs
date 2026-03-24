using System.IO;
using PDFConvert.Application.Interfaces;
using PDFConvert.Application.Services;
using PDFConvert.Infrastructure.Extraction;
using PDFConvert.Infrastructure.Storage;
using PDFConvert.Infrastructure.Storage.AppState;
using PDFConvert.Infrastructure.Writers;
using PDFConvert.App.Views;
using PDFConvert.App.Services;
using PDFConvert.Presentation.ViewModels;

namespace PDFConvert.App.Bootstrapping;

public sealed class Bootstrapper
{
    public MainWindow CreateMainWindow()
    {
        var appDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PDFConvert");
        var appStateStore = new JsonAppStateStore(Path.Combine(appDataDirectory, "appstate.json"));

        IFileDialogService fileDialogService = new FileDialogService();
        IFileSystemLauncher fileSystemLauncher = new FileSystemLauncher();
        IGitVersionService gitVersionService = new GitVersionService();
        IOcrSettingsStore ocrSettingsStore = new FileOcrSettingsStore(appStateStore);
        Environment.SetEnvironmentVariable("GOOGLE_VISION_API_KEY", ocrSettingsStore.GetGoogleVisionApiKey());
        Environment.SetEnvironmentVariable("NAVER_CLOVA_OCR_ENDPOINT", ocrSettingsStore.GetNaverClovaEndpoint());
        Environment.SetEnvironmentVariable("NAVER_CLOVA_OCR_SECRET", ocrSettingsStore.GetNaverClovaSecret());
        IRecentPathStore recentPathStore = new FileRecentPathStore(appStateStore);
        IRecentConversionStore recentConversionStore = new FileRecentConversionStore(appStateStore);
        IOutputPathResolver outputPathResolver = new OutputPathResolver();
        IPdfTextExtractor pdfTextExtractor = new PdfPigTextExtractor();
        IOutputWriter[] writers =
        [
            new DocxOutputWriter(),
            new PptxOutputWriter(),
            new DocOutputWriter(),
            new GoogleSlidesPlaceholderWriter(),
        ];
        IConversionCoordinator conversionCoordinator = new ConversionCoordinator(pdfTextExtractor, outputPathResolver, writers);
        var viewModel = new MainWindowViewModel(
            conversionCoordinator,
            fileDialogService,
            fileSystemLauncher,
            gitVersionService,
            ocrSettingsStore,
            recentConversionStore,
            recentPathStore);

        return new MainWindow
        {
            DataContext = viewModel,
        };
    }
}
