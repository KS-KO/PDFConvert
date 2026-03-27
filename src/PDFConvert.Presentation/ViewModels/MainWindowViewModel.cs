using System.Collections.ObjectModel;
using PDFConvert.Application.Interfaces;
using PDFConvert.Domain.Entities;
using PDFConvert.Domain.Enums;
using PDFConvert.Domain.ValueObjects;
using PDFConvert.Presentation.Commands;

namespace PDFConvert.Presentation.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly IConversionCoordinator _conversionCoordinator;
    private readonly IFileDialogService _fileDialogService;
    private readonly IFileSystemLauncher _fileSystemLauncher;
    private readonly IGitVersionService _gitVersionService;
    private readonly IOcrSettingsStore _ocrSettingsStore;
    private readonly IRecentConversionStore _recentConversionStore;
    private readonly IRecentPathStore _recentPathStore;

    private string? _selectedPdfPath;
    private bool _useOcr;
    private OcrEngineOptionViewModel? _selectedOcrEngine;
    private string? _googleVisionApiKey;
    private string? _googleClientId;
    private string? _googleClientSecret;
    private string? _naverClovaEndpoint;
    private string? _naverClovaSecret;
    private int _progressValue;
    private string _statusMessage = "PDF 파일을 선택해 주세요.";
    private bool _isBusy;
    private string _resultSummary = "아직 변환을 시작하지 않았습니다.";
    private string? _selectedOutputDirectory;

    public MainWindowViewModel(
        IConversionCoordinator conversionCoordinator,
        IFileDialogService fileDialogService,
        IFileSystemLauncher fileSystemLauncher,
        IGitVersionService gitVersionService,
        IOcrSettingsStore ocrSettingsStore,
        IRecentConversionStore recentConversionStore,
        IRecentPathStore recentPathStore)
    {
        _conversionCoordinator = conversionCoordinator;
        _fileDialogService = fileDialogService;
        _fileSystemLauncher = fileSystemLauncher;
        _gitVersionService = gitVersionService;
        _ocrSettingsStore = ocrSettingsStore;
        _recentConversionStore = recentConversionStore;
        _recentPathStore = recentPathStore;

        GeneratedFiles = new ObservableCollection<string>();
        RecentConversions = new ObservableCollection<RecentConversionItem>();
        Warnings = new ObservableCollection<string>();
        OcrEngines = new ObservableCollection<OcrEngineOptionViewModel>
        {
            new(OcrEngineKind.Auto, "Auto"),
            new(OcrEngineKind.WindowsKoreanPreferred, "Basic OCR (Korean Preferred)"),
            new(OcrEngineKind.WindowsEnglishPreferred, "Basic OCR (English Preferred)"),
            new(OcrEngineKind.Tesseract, "Tesseract"),
            new(OcrEngineKind.GoogleVisionOcr, "Google Vision OCR"),
            new(OcrEngineKind.NaverClovaOcr, "Naver Clova OCR"),
        };
        _selectedOcrEngine = OcrEngines[0];

        OutputFormats = new ObservableCollection<OutputFormatOptionViewModel>
        {
            new(OutputFormat.Pptx, "PPT", isSelected: true),
            new(OutputFormat.Pptx, "PPTX"),
            new(OutputFormat.Docx, "DOCX"),
            new(OutputFormat.Doc, "DOC"),
            new(OutputFormat.GoogleSlides, "Google Slides"),
            new(OutputFormat.PythonPptx, "Python (Editable PPTX)"),
        };

        foreach (var format in OutputFormats)
        {
            format.PropertyChanged += (_, _) => OnStateChanged();
        }

        BrowseFileCommand = new AsyncRelayCommand(BrowseFileAsync, () => !IsBusy);
        BrowseOutputFolderCommand = new AsyncRelayCommand(BrowseOutputFolderAsync, () => !IsBusy);
        StartConversionCommand = new AsyncRelayCommand(StartConversionAsync, () => CanStartConversion);
        OpenOutputFolderCommand = new RelayCommand(OpenOutputFolder, () => !string.IsNullOrWhiteSpace(OutputDirectory));
        SaveOcrSettingsCommand = new RelayCommand(SaveOcrSettings);

        var recentPath = _recentPathStore.GetLastSelectedPdfPath();
        if (!string.IsNullOrWhiteSpace(recentPath))
        {
            SelectedPdfPath = recentPath;
            StatusMessage = "최근 선택한 PDF 경로를 복원했습니다.";
        }

        SelectedOutputDirectory = _recentPathStore.GetLastSelectedOutputDirectory();

        foreach (var item in _recentConversionStore.LoadRecentConversions())
        {
            RecentConversions.Add(item);
        }

        GoogleVisionApiKey = _ocrSettingsStore.GetGoogleVisionApiKey();
        GoogleClientId = _ocrSettingsStore.GetGoogleClientId();
        GoogleClientSecret = _ocrSettingsStore.GetGoogleClientSecret();
        NaverClovaEndpoint = _ocrSettingsStore.GetNaverClovaEndpoint();
        NaverClovaSecret = _ocrSettingsStore.GetNaverClovaSecret();
        ApplyOcrEnvironmentSettings();

        var gitVersion = _gitVersionService.GetVersionInfo();
        CommitCount = gitVersion.CommitCountDisplay;
        CommitHashShort = gitVersion.ShortHashDisplay;
    }

    public ObservableCollection<OutputFormatOptionViewModel> OutputFormats { get; }

    public AsyncRelayCommand BrowseFileCommand { get; }

    public AsyncRelayCommand BrowseOutputFolderCommand { get; }

    public AsyncRelayCommand StartConversionCommand { get; }

    public RelayCommand OpenOutputFolderCommand { get; }

    public RelayCommand SaveOcrSettingsCommand { get; }

    public ObservableCollection<string> GeneratedFiles { get; }

    public ObservableCollection<RecentConversionItem> RecentConversions { get; }

    public ObservableCollection<string> Warnings { get; }

    public ObservableCollection<OcrEngineOptionViewModel> OcrEngines { get; }

    public string CommitCount { get; }

    public string CommitHashShort { get; }

    public string GitVersionDisplay => $"Commit Count: {CommitCount} | hash: {CommitHashShort}";

    public string? GoogleVisionApiKey
    {
        get => _googleVisionApiKey;
        set => SetProperty(ref _googleVisionApiKey, value);
    }

    public string? GoogleClientId
    {
        get => _googleClientId;
        set => SetProperty(ref _googleClientId, value);
    }

    public string? GoogleClientSecret
    {
        get => _googleClientSecret;
        set => SetProperty(ref _googleClientSecret, value);
    }

    public string? NaverClovaEndpoint
    {
        get => _naverClovaEndpoint;
        set => SetProperty(ref _naverClovaEndpoint, value);
    }

    public string? NaverClovaSecret
    {
        get => _naverClovaSecret;
        set => SetProperty(ref _naverClovaSecret, value);
    }

    public string? SelectedPdfPath
    {
        get => _selectedPdfPath;
        private set
        {
            if (SetProperty(ref _selectedPdfPath, value))
            {
                OnPropertyChanged(nameof(SelectedPdfName));
                OnStateChanged();
            }
        }
    }

    public string SelectedPdfName => string.IsNullOrWhiteSpace(SelectedPdfPath)
        ? "선택된 파일 없음"
        : Path.GetFileName(SelectedPdfPath);

    public bool UseOcr
    {
        get => _useOcr;
        set
        {
            if (SetProperty(ref _useOcr, value))
            {
                OnPropertyChanged(nameof(IsOcrEngineSelectionVisible));
            }
        }
    }

    public OcrEngineOptionViewModel? SelectedOcrEngine
    {
        get => _selectedOcrEngine;
        set => SetProperty(ref _selectedOcrEngine, value);
    }

    public bool IsOcrEngineSelectionVisible => UseOcr;

    public string? SelectedOutputDirectory
    {
        get => _selectedOutputDirectory;
        private set => SetProperty(ref _selectedOutputDirectory, value);
    }

    public int ProgressValue
    {
        get => _progressValue;
        private set => SetProperty(ref _progressValue, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string ResultSummary
    {
        get => _resultSummary;
        private set => SetProperty(ref _resultSummary, value);
    }

    public string? OutputDirectory => GeneratedFiles.Count == 0
        ? SelectedOutputDirectory
        : Path.GetDirectoryName(GeneratedFiles[0]);

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnStateChanged();
            }
        }
    }

    public bool CanStartConversion => !IsBusy &&
                                      !string.IsNullOrWhiteSpace(SelectedPdfPath) &&
                                      OutputFormats.Any(item => item.IsSelected);

    private Task BrowseFileAsync()
    {
        try
        {
            var selectedPath = _fileDialogService.PickPdfFile(GetInitialPdfDirectory());
            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                return Task.CompletedTask;
            }

            SelectedPdfPath = selectedPath;

            try
            {
                _recentPathStore.SaveLastSelectedPdfPath(selectedPath);
            }
            catch (Exception ex)
            {
                StatusMessage = "PDF 파일은 선택되었지만 최근 경로 저장에 실패했습니다.";
                ResultSummary = ex.Message;
                return Task.CompletedTask;
            }

            StatusMessage = "변환할 PDF 파일을 선택했습니다.";
        }
        catch (Exception ex)
        {
            StatusMessage = "PDF 파일 선택 중 오류가 발생했습니다.";
            ResultSummary = ex.Message;
        }

        return Task.CompletedTask;
    }

    private Task BrowseOutputFolderAsync()
    {
        try
        {
            var selectedPath = _fileDialogService.PickOutputFolder(SelectedOutputDirectory);
            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                return Task.CompletedTask;
            }

            SelectedOutputDirectory = selectedPath;

            try
            {
                _recentPathStore.SaveLastSelectedOutputDirectory(selectedPath);
            }
            catch (Exception ex)
            {
                StatusMessage = "출력 폴더는 선택되었지만 최근 경로 저장에 실패했습니다.";
                ResultSummary = ex.Message;
                OnStateChanged();
                return Task.CompletedTask;
            }

            StatusMessage = "결과 저장 폴더를 선택했습니다.";
            OnStateChanged();
        }
        catch (Exception ex)
        {
            StatusMessage = "출력 폴더 선택 중 오류가 발생했습니다.";
            ResultSummary = ex.Message;
        }

        return Task.CompletedTask;
    }

    private async Task StartConversionAsync()
    {
        if (!CanStartConversion || SelectedPdfPath is null)
        {
            return;
        }

        IsBusy = true;
        ProgressValue = 0;
        ResultSummary = "변환을 시작했습니다.";
        GeneratedFiles.Clear();
        Warnings.Clear();

        try
        {
            var selectedFormats = OutputFormats
                .Where(item => item.IsSelected)
                .Select(item => item.Format)
                .Distinct()
                .ToArray();

            var job = new ConversionJob
            {
                SourceFilePath = SelectedPdfPath,
                TargetFormats = selectedFormats,
                OutputDirectory = SelectedOutputDirectory,
                UseOcr = UseOcr,
                OcrEngineKind = SelectedOcrEngine?.EngineKind ?? OcrEngineKind.Auto,
            };

            var progress = new Progress<ConversionProgress>(UpdateProgress);
            var result = await _conversionCoordinator.ConvertAsync(job, progress);

            ResultSummary = result.SummaryMessage;
            foreach (var artifact in result.OutputArtifacts)
            {
                GeneratedFiles.Add(artifact);
            }

            foreach (var warning in result.Warnings)
            {
                Warnings.Add(warning);
            }

            if (!string.IsNullOrWhiteSpace(OutputDirectory))
            {
                RecentConversions.Insert(0, new RecentConversionItem
                {
                    ConvertedAt = DateTimeOffset.Now,
                    SourceFileName = Path.GetFileName(SelectedPdfPath),
                    OutputDirectory = OutputDirectory,
                    Summary = result.SummaryMessage,
                });
                TrimRecentConversions();
                _recentPathStore.SaveLastSelectedOutputDirectory(OutputDirectory);
                _recentConversionStore.SaveRecentConversions(RecentConversions.ToList());
            }

            OnPropertyChanged(nameof(OutputDirectory));
        }
        catch (Exception ex)
        {
            StatusMessage = "변환 중 오류가 발생했습니다.";
            ResultSummary = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateProgress(ConversionProgress progress)
    {
        ProgressValue = progress.Percent;
        StatusMessage = progress.Message;
    }

    private void OnStateChanged()
    {
        BrowseFileCommand.RaiseCanExecuteChanged();
        BrowseOutputFolderCommand.RaiseCanExecuteChanged();
        StartConversionCommand.RaiseCanExecuteChanged();
        OpenOutputFolderCommand.RaiseCanExecuteChanged();
        OnPropertyChanged(nameof(CanStartConversion));
        OnPropertyChanged(nameof(OutputDirectory));
    }

    private void OpenOutputFolder()
    {
        if (!string.IsNullOrWhiteSpace(OutputDirectory))
        {
            _fileSystemLauncher.OpenFolder(OutputDirectory);
        }
    }

    private void SaveOcrSettings()
    {
        _ocrSettingsStore.SaveGoogleVisionApiKey(GoogleVisionApiKey);
        _ocrSettingsStore.SaveGoogleClientId(GoogleClientId);
        _ocrSettingsStore.SaveGoogleClientSecret(GoogleClientSecret);
        _ocrSettingsStore.SaveNaverClovaEndpoint(NaverClovaEndpoint);
        _ocrSettingsStore.SaveNaverClovaSecret(NaverClovaSecret);
        ApplyOcrEnvironmentSettings();
        StatusMessage = "Settings saved.";
    }

    private void ApplyOcrEnvironmentSettings()
    {
        Environment.SetEnvironmentVariable("GOOGLE_VISION_API_KEY", GoogleVisionApiKey);
        Environment.SetEnvironmentVariable("NAVER_CLOVA_OCR_ENDPOINT", NaverClovaEndpoint);
        Environment.SetEnvironmentVariable("NAVER_CLOVA_OCR_SECRET", NaverClovaSecret);
    }

    private void TrimRecentConversions()
    {
        const int maxItems = 10;
        while (RecentConversions.Count > maxItems)
        {
            RecentConversions.RemoveAt(RecentConversions.Count - 1);
        }
    }

    private string? GetInitialPdfDirectory()
    {
        if (!string.IsNullOrWhiteSpace(SelectedPdfPath))
        {
            var selectedPdfDirectory = Path.GetDirectoryName(SelectedPdfPath);
            if (!string.IsNullOrWhiteSpace(selectedPdfDirectory))
            {
                return selectedPdfDirectory;
            }
        }

        return SelectedOutputDirectory;
    }
}
