using Tesseract;

namespace PDFConvert.Infrastructure.Extraction;

internal sealed class TesseractOcrImageTextRecognizer
{
    private readonly Lazy<string?> _tessdataPath = new(() => TesseractRuntimeLocator.ResolveTessdataDirectory());

    public bool IsAvailable() => !string.IsNullOrWhiteSpace(_tessdataPath.Value);

    public async Task<string?> RecognizeAsync(byte[] imageBytes, CancellationToken cancellationToken = default)
    {
        var tessdataPath = _tessdataPath.Value;
        if (string.IsNullOrWhiteSpace(tessdataPath) || imageBytes.Length == 0)
        {
            return null;
        }

        try
        {
            // Tesseract library is CPU bound and synchronous, so we run it in Task.Run
            return await Task.Run(() =>
            {
                try
                {
                    // Ensure our kor+eng data exists in the path
                    // Use Default engine mode
                    using var engine = new TesseractEngine(tessdataPath, "kor+eng", EngineMode.Default);
                    
                    // Pix.LoadFromMemory is fast
                    using var img = Pix.LoadFromMemory(imageBytes);
                    
                    using var page = engine.Process(img, PageSegMode.Auto);
                    
                    var text = page.GetText();
                    return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
                }
                catch (Exception)
                {
                    // If kor+eng fails, try just eng or kor
                    try
                    {
                        using var engine = new TesseractEngine(tessdataPath, "eng", EngineMode.Default);
                        using var img = Pix.LoadFromMemory(imageBytes);
                        using var page = engine.Process(img);
                        var text = page.GetText();
                        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
                    }
                    catch { return null; }
                }
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
