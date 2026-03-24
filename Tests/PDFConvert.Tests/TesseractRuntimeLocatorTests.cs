using PDFConvert.Infrastructure.Extraction;

namespace PDFConvert.Tests;

public sealed class TesseractRuntimeLocatorTests
{
    [Fact]
    public void ResolveExecutablePath_Prefers_Bundled_Runtime_Under_App_Base_Directory()
    {
        var baseDirectory = Path.Combine(Path.GetTempPath(), $"pdfconvert_tesseract_{Guid.NewGuid():N}");
        var bundledDirectory = Path.Combine(baseDirectory, "tools", "tesseract");
        Directory.CreateDirectory(bundledDirectory);

        var executablePath = Path.Combine(bundledDirectory, "tesseract.exe");
        File.WriteAllText(executablePath, string.Empty);

        try
        {
            var resolvedPath = TesseractRuntimeLocator.ResolveExecutablePath(baseDirectory);

            Assert.Equal(Path.GetFullPath(executablePath), resolvedPath);
        }
        finally
        {
            Directory.Delete(baseDirectory, recursive: true);
        }
    }

    [Fact]
    public void ResolveTessdataDirectory_Returns_Sidecar_Tessdata_Folder()
    {
        var baseDirectory = Path.Combine(Path.GetTempPath(), $"pdfconvert_tessdata_{Guid.NewGuid():N}");
        var bundledDirectory = Path.Combine(baseDirectory, "tools", "tesseract");
        var tessdataDirectory = Path.Combine(bundledDirectory, "tessdata");
        Directory.CreateDirectory(tessdataDirectory);

        var executablePath = Path.Combine(bundledDirectory, "tesseract.exe");
        File.WriteAllText(executablePath, string.Empty);

        try
        {
            var resolvedPath = TesseractRuntimeLocator.ResolveTessdataDirectory(executablePath);

            Assert.Equal(Path.GetFullPath(tessdataDirectory), resolvedPath);
        }
        finally
        {
            Directory.Delete(baseDirectory, recursive: true);
        }
    }
}
