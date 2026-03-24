namespace PDFConvert.Application.Interfaces;

public interface IRecentPathStore
{
    string? GetLastSelectedPdfPath();
    string? GetLastSelectedOutputDirectory();
    void SaveLastSelectedPdfPath(string pdfPath);
    void SaveLastSelectedOutputDirectory(string outputDirectory);
}
