namespace PDFConvert.Infrastructure.Extraction;

internal static class TesseractRuntimeLocator
{
    public static string? ResolveTessdataDirectory()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("TESSDATA_PREFIX"),
            Path.Combine(baseDirectory, "tools", "tesseract", "tessdata"),
            Path.Combine(baseDirectory, "tessdata"),
            Path.Combine(Directory.GetCurrentDirectory(), "tools", "tesseract", "tessdata"),
            Path.Combine(Directory.GetCurrentDirectory(), "tessdata"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Tesseract-OCR", "tessdata"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Tesseract-OCR", "tessdata"),
        };

        foreach (var candidate in candidates.Where(candidate => !string.IsNullOrWhiteSpace(candidate)))
        {
            var fullPath = Path.GetFullPath(candidate!);
            if (Directory.Exists(fullPath) && (File.Exists(Path.Combine(fullPath, "eng.traineddata")) || File.Exists(Path.Combine(fullPath, "kor.traineddata"))))
            {
                return fullPath;
            }
        }

        return null;
    }
}
