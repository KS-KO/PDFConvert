namespace PDFConvert.Application.Interfaces;

public interface IOutputPathResolver
{
    string GetOutputDirectory(string sourcePdfPath, string? preferredOutputDirectory = null);
}
