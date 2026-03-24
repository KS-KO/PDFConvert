using System.Diagnostics;

namespace PDFConvert.Infrastructure.Extraction;

internal sealed class TesseractOcrImageTextRecognizer
{
    private readonly Lazy<string?> _executablePath = new(() => TesseractRuntimeLocator.ResolveExecutablePath());

    public bool IsAvailable() => !string.IsNullOrWhiteSpace(_executablePath.Value);

    public async Task<string?> RecognizeAsync(byte[] imageBytes, CancellationToken cancellationToken = default)
    {
        var executablePath = _executablePath.Value;
        if (string.IsNullOrWhiteSpace(executablePath) || imageBytes.Length == 0)
        {
            return null;
        }

        var tempFilePath = Path.Combine(Path.GetTempPath(), $"pdfconvert_{Guid.NewGuid():N}.png");

        try
        {
            await File.WriteAllBytesAsync(tempFilePath, imageBytes, cancellationToken);

            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = $"\"{tempFilePath}\" stdout -l kor+eng --psm 6",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(executablePath) ?? AppContext.BaseDirectory,
            };

            var tessdataDirectory = TesseractRuntimeLocator.ResolveTessdataDirectory(executablePath);
            if (!string.IsNullOrWhiteSpace(tessdataDirectory))
            {
                startInfo.Environment["TESSDATA_PREFIX"] = tessdataDirectory;
            }

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return null;
            }

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            return process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output)
                ? output.Trim()
                : null;
        }
        catch
        {
            return null;
        }
        finally
        {
            try
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
            catch
            {
            }
        }
    }
}
