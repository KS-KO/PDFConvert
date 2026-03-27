using System.Diagnostics;

namespace PDFConvert.Infrastructure.Writers;

internal static class PythonScriptLocator
{
    public static string? ResolveScriptPath(string scriptName, string? applicationBaseDirectory = null)
    {
        var exeName = Path.ChangeExtension(scriptName, ".exe").Replace("_editable", ""); 
        var baseDirectory = applicationBaseDirectory ?? AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDirectory, "tools", "python_pptx_converter", "pdf_to_pptx_converter.exe"),
            Path.Combine(Directory.GetCurrentDirectory(), "tools", "python_pptx_converter", "pdf_to_pptx_converter.exe"),
            Path.Combine(baseDirectory, "tools", "python_pptx_converter", scriptName),
            Path.Combine(Directory.GetCurrentDirectory(), "tools", "python_pptx_converter", scriptName),
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }
        }

        return null;
    }

    public static string? ResolvePythonExecutable()
    {
        var candidates = new[] { "python", "python3", "py" };
        foreach (var candidate in candidates)
        {
            if (CanExecute(candidate))
            {
                return candidate;
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

            process.WaitForExit(1000);
            return true; // Just checking if it can be started
        }
        catch
        {
            return false;
        }
    }
}
