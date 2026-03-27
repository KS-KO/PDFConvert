using PDFConvert.Infrastructure.Extraction;

namespace PDFConvert.Tests;

public sealed class TesseractRuntimeLocatorTests
{
    [Fact]
    public void ResolveTessdataDirectory_Finds_Tessdata_In_Tools()
    {
        var baseDir = AppContext.BaseDirectory;
        var toolsDir = Path.Combine(baseDir, "tools", "tesseract", "tessdata");
        Directory.CreateDirectory(toolsDir);
        File.WriteAllText(Path.Combine(toolsDir, "eng.traineddata"), "");

        try
        {
            var resolved = TesseractRuntimeLocator.ResolveTessdataDirectory();
            Assert.NotNull(resolved);
            Assert.Contains("tessdata", resolved, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            // Clean up if needed, but AppContext.BaseDirectory might be shared
        }
    }
}
