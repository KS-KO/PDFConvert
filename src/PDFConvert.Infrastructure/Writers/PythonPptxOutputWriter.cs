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

        if (isExe)
        {
            fileName = scriptPath;
            arguments = $"--pdf \"{sourceFilePath}\" --outdir \"{outputDirectory}\"";
        }
        else
        {
            var pythonExecutable = PythonScriptLocator.ResolvePythonExecutable();
            if (pythonExecutable is null)
            {
                throw new Exception("Python was not found. Please install Python or use the bundled version.");
            }
            fileName = pythonExecutable;
            arguments = $"\"{scriptPath}\" --pdf \"{sourceFilePath}\" --outdir \"{outputDirectory}\"";
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

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        var error = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            throw new Exception($"Python conversion failed with exit code {process.ExitCode}.\nError: {error}");
        }

        if (!File.Exists(expectedOutputPath))
        {
            throw new FileNotFoundException($"Python script completed but the expected output file was not found: {expectedOutputPath}");
        }

        return expectedOutputPath;
    }
}
