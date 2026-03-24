using PDFConvert.Application.Interfaces;

namespace PDFConvert.Infrastructure.Storage;

public sealed class OutputPathResolver : IOutputPathResolver
{
    public string GetOutputDirectory(string sourcePdfPath, string? preferredOutputDirectory = null)
    {
        if (!string.IsNullOrWhiteSpace(preferredOutputDirectory))
        {
            return preferredOutputDirectory;
        }

        var sourceDirectory = Path.GetDirectoryName(sourcePdfPath) ?? Environment.CurrentDirectory;
        return Path.Combine(sourceDirectory, "output");
    }
}
