using PDFConvert.Application.Interfaces;

namespace PDFConvert.Infrastructure.Storage;

public sealed class InMemoryRecentPathStore : IRecentPathStore
{
    private string? _lastSelectedPdfPath;
    private string? _lastSelectedOutputDirectory;

    public string? GetLastSelectedPdfPath() => _lastSelectedPdfPath;

    public string? GetLastSelectedOutputDirectory() => _lastSelectedOutputDirectory;

    public void SaveLastSelectedPdfPath(string pdfPath)
    {
        _lastSelectedPdfPath = pdfPath;
    }

    public void SaveLastSelectedOutputDirectory(string outputDirectory)
    {
        _lastSelectedOutputDirectory = outputDirectory;
    }
}
