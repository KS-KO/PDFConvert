using PDFConvert.Application.Interfaces;
using PDFConvert.Infrastructure.Storage.AppState;

namespace PDFConvert.Infrastructure.Storage;

public sealed class FileRecentPathStore : IRecentPathStore
{
    private readonly JsonAppStateStore _stateStore;

    public FileRecentPathStore(JsonAppStateStore stateStore)
    {
        _stateStore = stateStore;
    }

    public string? GetLastSelectedSourcePath() => _stateStore.Load().LastSelectedPdfPath;

    public string? GetLastSelectedOutputDirectory() => _stateStore.Load().LastSelectedOutputDirectory;

    public void SaveLastSelectedSourcePath(string sourcePath)
    {
        var state = _stateStore.Load();
        state.LastSelectedPdfPath = sourcePath;
        _stateStore.Save(state);
    }

    public void SaveLastSelectedOutputDirectory(string outputDirectory)
    {
        var state = _stateStore.Load();
        state.LastSelectedOutputDirectory = outputDirectory;
        _stateStore.Save(state);
    }
}
