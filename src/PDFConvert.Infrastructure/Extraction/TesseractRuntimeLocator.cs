using System.Diagnostics;

namespace PDFConvert.Infrastructure.Extraction;

internal static class TesseractRuntimeLocator
{
    public static string? ResolveExecutablePath(string? applicationBaseDirectory = null)
    {
        var baseDirectory = applicationBaseDirectory ?? AppContext.BaseDirectory;
        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("TESSERACT_PATH"),
            Path.Combine(baseDirectory, "tools", "tesseract", "tesseract.exe"),
            Path.Combine(baseDirectory, "tesseract", "tesseract.exe"),
            Path.Combine(Directory.GetCurrentDirectory(), "tools", "tesseract", "tesseract.exe"),
            Path.Combine(Directory.GetCurrentDirectory(), "tesseract", "tesseract.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Tesseract-OCR", "tesseract.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Tesseract-OCR", "tesseract.exe"),
            "tesseract",
        };

        foreach (var candidate in candidates.Where(candidate => !string.IsNullOrWhiteSpace(candidate)))
        {
            if (candidate!.Contains(Path.DirectorySeparatorChar) || candidate.Contains(Path.AltDirectorySeparatorChar))
            {
                if (File.Exists(candidate))
                {
                    return Path.GetFullPath(candidate);
                }

                continue;
            }

            if (CanExecute(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    public static string? ResolveTessdataDirectory(string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return null;
        }

        var executableDirectory = Path.GetDirectoryName(executablePath);
        if (string.IsNullOrWhiteSpace(executableDirectory))
        {
            return null;
        }

        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("TESSDATA_PREFIX"),
            Path.Combine(executableDirectory, "tessdata"),
            Path.Combine(executableDirectory, "..", "tessdata"),
        };

        foreach (var candidate in candidates.Where(candidate => !string.IsNullOrWhiteSpace(candidate)))
        {
            var fullPath = Path.GetFullPath(candidate!);
            if (Directory.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return null;
    }

    private static bool CanExecute(string fileName)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return false;
            }

            process.WaitForExit(2000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
