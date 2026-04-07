namespace PDFConvert.Application.Interfaces;

public interface IRecentPathStore
{
    string? GetLastSelectedSourcePath();
    string? GetLastSelectedOutputDirectory();
    void SaveLastSelectedSourcePath(string sourcePath);
    void SaveLastSelectedOutputDirectory(string outputDirectory);
}
