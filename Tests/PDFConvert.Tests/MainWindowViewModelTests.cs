using PDFConvert.Application.Interfaces;
using PDFConvert.Domain.Entities;
using PDFConvert.Domain.Enums;
using PDFConvert.Domain.ValueObjects;
using PDFConvert.Presentation.ViewModels;

namespace PDFConvert.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void Constructor_Sets_Default_Selections()
    {
        var viewModel = new MainWindowViewModel(
            new FakeConversionCoordinator(),
            new FakeFileDialogService(),
            new FakeFileSystemLauncher(),
            new FakeGitVersionService(),
            new FakeOcrSettingsStore(),
            new FakeRecentConversionStore(),
            new FakeRecentPathStore());

        Assert.Equal("선택된 파일 없음", viewModel.SelectedPdfName);
        Assert.Contains(viewModel.OutputFormats, item => item.DisplayName == "PPT" && item.IsSelected);
        Assert.Contains(viewModel.OutputFormats, item => item.DisplayName == "PPTX");
        Assert.Equal("Commit Count: 321 | hash: 123456789", viewModel.GitVersionDisplay);
        Assert.Equal(OcrEngineKind.Auto, viewModel.SelectedOcrEngine?.EngineKind);
        Assert.Contains(viewModel.OcrEngines, item => item.EngineKind == OcrEngineKind.Tesseract);
        Assert.Contains(viewModel.OcrEngines, item => item.EngineKind == OcrEngineKind.GoogleVisionOcr);
        Assert.Contains(viewModel.OcrEngines, item => item.EngineKind == OcrEngineKind.NaverClovaOcr);
    }

    [Fact]
    public void Constructor_Loads_Recent_State()
    {
        var recentItems = new[]
        {
            new RecentConversionItem
            {
                ConvertedAt = DateTimeOffset.Now,
                SourceFileName = "sample.pdf",
                OutputDirectory = "C:\\output",
                Summary = "done",
            },
        };

        var viewModel = new MainWindowViewModel(
            new FakeConversionCoordinator(),
            new FakeFileDialogService(),
            new FakeFileSystemLauncher(),
            new FakeGitVersionService(),
            new FakeOcrSettingsStore(),
            new FakeRecentConversionStore(recentItems),
            new FakeRecentPathStore(
                lastSelectedPdfPath: "C:\\docs\\sample.pdf",
                lastSelectedOutputDirectory: "C:\\output"));

        Assert.Equal("sample.pdf", viewModel.SelectedPdfName);
        Assert.Equal("C:\\output", viewModel.SelectedOutputDirectory);
        Assert.Single(viewModel.RecentConversions);
    }

    private sealed class FakeGitVersionService : IGitVersionService
    {
        public GitVersionInfo GetVersionInfo() => new(321, "123456789");
    }

    private sealed class FakeOcrSettingsStore : IOcrSettingsStore
    {
        public string? GetGoogleVisionApiKey() => null;
        public void SaveGoogleVisionApiKey(string? apiKey) { }
        public string? GetGoogleClientId() => null;
        public void SaveGoogleClientId(string? clientId) { }
        public string? GetGoogleClientSecret() => null;
        public void SaveGoogleClientSecret(string? clientSecret) { }
        public string? GetNaverClovaEndpoint() => null;
        public void SaveNaverClovaEndpoint(string? endpoint) { }
        public string? GetNaverClovaSecret() => null;
        public void SaveNaverClovaSecret(string? secret) { }
    }

    private sealed class FakeConversionCoordinator : IConversionCoordinator
    {
        public Task<ConversionResult> ConvertAsync(
            ConversionJob job,
            IProgress<ConversionProgress> progress,
            CancellationToken cancellationToken = default)
        {
            progress.Report(new ConversionProgress(ConversionStatus.Completed, 100, "done"));
            return Task.FromResult(new ConversionResult
            {
                IsSuccess = true,
                SummaryMessage = "ok",
                OutputArtifacts = ["sample.docx"],
            });
        }
    }

    private sealed class FakeFileDialogService : IFileDialogService
    {
        public string? PickPdfFile(string? initialDirectory = null) => null;

        public string? PickOutputFolder(string? initialDirectory = null) => null;
    }

    private sealed class FakeFileSystemLauncher : IFileSystemLauncher
    {
        public void OpenFolder(string folderPath)
        {
        }
    }

    private sealed class FakeRecentPathStore : IRecentPathStore
    {
        private readonly string? _lastSelectedPdfPath;
        private readonly string? _lastSelectedOutputDirectory;

        public FakeRecentPathStore(
            string? lastSelectedPdfPath = null,
            string? lastSelectedOutputDirectory = null)
        {
            _lastSelectedPdfPath = lastSelectedPdfPath;
            _lastSelectedOutputDirectory = lastSelectedOutputDirectory;
        }

        public string? GetLastSelectedPdfPath() => _lastSelectedPdfPath;

        public string? GetLastSelectedOutputDirectory() => _lastSelectedOutputDirectory;

        public void SaveLastSelectedPdfPath(string pdfPath)
        {
        }

        public void SaveLastSelectedOutputDirectory(string outputDirectory)
        {
        }
    }

    private sealed class FakeRecentConversionStore : IRecentConversionStore
    {
        private readonly IReadOnlyList<RecentConversionItem> _items;

        public FakeRecentConversionStore(IReadOnlyList<RecentConversionItem>? items = null)
        {
            _items = items ?? [];
        }

        public IReadOnlyList<RecentConversionItem> LoadRecentConversions() => _items;

        public void SaveRecentConversions(IReadOnlyList<RecentConversionItem> items)
        {
        }
    }
}
