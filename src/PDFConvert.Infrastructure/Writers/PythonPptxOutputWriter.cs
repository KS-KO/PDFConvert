using System.Diagnostics;
using PDFConvert.Application.Interfaces;
using PDFConvert.Domain.Entities;
using PDFConvert.Domain.Enums;

namespace PDFConvert.Infrastructure.Writers;

public sealed class PythonPptxOutputWriter : IOutputWriter
{
    public OutputFormat Format => OutputFormat.PythonPptx;

    public async Task<string> WriteAsync(PdfDocumentContent document, string outputDirectory, CancellationToken cancellationToken = default)
    {
        var sourceFilePath = document.SourceFilePath;
        if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException($"Source PDF file not found: {sourceFilePath}");
        }

        var expectedOutputFilename = Path.GetFileNameWithoutExtension(sourceFilePath) + "_editable.pptx";
        var expectedOutputPath = Path.Combine(outputDirectory, expectedOutputFilename);

        var scriptPath = PythonScriptLocator.ResolveScriptPath("pdf_to_pptx_editable.py");
        if (scriptPath is null)
        {
            throw new FileNotFoundException("Python conversion engine not found.");
        }

        var isExe = scriptPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
        string fileName;
        string arguments;

        var outputDirTrimmed = outputDirectory.TrimEnd('\\', '/');
        var sourceFileTrimmed = sourceFilePath.TrimEnd('\\', '/');

        if (isExe)
        {
            fileName = scriptPath;
            arguments = $"--pdf \"{sourceFileTrimmed}\" --outdir \"{outputDirTrimmed}\"";
        }
        else
        {
            var pythonExecutable = PythonScriptLocator.ResolvePythonExecutable();
            if (pythonExecutable is null)
            {
                throw new Exception("Python was not found. Please install Python or use the bundled version.");
            }
            fileName = pythonExecutable;
            arguments = $"\"{scriptPath}\" --pdf \"{sourceFileTrimmed}\" --outdir \"{outputDirTrimmed}\"";
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            throw new Exception("Failed to start the Python process.");
        }

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        var output = await outputTask.ConfigureAwait(false);
        var error = await errorTask.ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            throw new Exception($"Python conversion failed with exit code {process.ExitCode}.\nError: {error}");
        }

        if (!File.Exists(expectedOutputPath))
        {
            var logPath = Path.Combine(outputDirectory, "python_conversion_error.log");
            File.WriteAllText(logPath, $"Standard Output:\n{output}\n\nStandard Error:\n{error}");
            throw new FileNotFoundException($"Python script completed but the expected output file was not found: {expectedOutputPath}\nDetailed log written to: {logPath}");
        }

        return expectedOutputPath;
    }
}
