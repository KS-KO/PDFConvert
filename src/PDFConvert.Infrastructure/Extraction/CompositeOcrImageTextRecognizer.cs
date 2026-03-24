using PDFConvert.Domain.Enums;

namespace PDFConvert.Infrastructure.Extraction;

internal sealed class CompositeOcrImageTextRecognizer
{
    private readonly WindowsOcrImageTextRecognizer _windowsRecognizer = new();
    private readonly TesseractOcrImageTextRecognizer _tesseractRecognizer = new();
    private readonly GoogleVisionOcrImageTextRecognizer _googleVisionRecognizer = new();
    private readonly NaverClovaOcrImageTextRecognizer _naverClovaRecognizer = new();

    public bool IsAvailable(OcrEngineKind engineKind)
    {
        return engineKind switch
        {
            OcrEngineKind.Auto => IsAvailable(OcrEngineKind.WindowsKoreanPreferred)
                                  || IsAvailable(OcrEngineKind.Tesseract)
                                  || IsAvailable(OcrEngineKind.GoogleVisionOcr)
                                  || IsAvailable(OcrEngineKind.NaverClovaOcr),
            OcrEngineKind.WindowsKoreanPreferred or OcrEngineKind.WindowsEnglishPreferred => _windowsRecognizer.IsAvailable(engineKind),
            OcrEngineKind.Tesseract => _tesseractRecognizer.IsAvailable(),
            OcrEngineKind.GoogleVisionOcr => _googleVisionRecognizer.IsAvailable(),
            OcrEngineKind.NaverClovaOcr => _naverClovaRecognizer.IsAvailable(),
            _ => false,
        };
    }

    public async Task<string?> RecognizeAsync(
        byte[] imageBytes,
        OcrEngineKind engineKind,
        CancellationToken cancellationToken = default)
    {
        if (imageBytes.Length == 0)
        {
            return null;
        }

        if (engineKind == OcrEngineKind.Auto)
        {
            return await RecognizeWithAutoAsync(imageBytes, cancellationToken);
        }

        var selectedResult = await RecognizeWithEngineAsync(imageBytes, engineKind, cancellationToken);
        if (!string.IsNullOrWhiteSpace(selectedResult))
        {
            return selectedResult;
        }

        return await RecognizeWithAutoAsync(imageBytes, cancellationToken);
    }

    public async Task<OcrPageLayout?> RecognizeLayoutAsync(
        byte[] imageBytes,
        OcrEngineKind engineKind,
        CancellationToken cancellationToken = default)
    {
        if (imageBytes.Length == 0)
        {
            return null;
        }

        if (engineKind is OcrEngineKind.WindowsKoreanPreferred or OcrEngineKind.WindowsEnglishPreferred)
        {
            return await _windowsRecognizer.RecognizeLayoutAsync(imageBytes, engineKind, cancellationToken);
        }

        var koreanLayout = await _windowsRecognizer.RecognizeLayoutAsync(imageBytes, OcrEngineKind.WindowsKoreanPreferred, cancellationToken);
        if (koreanLayout is not null)
        {
            return koreanLayout;
        }

        return await _windowsRecognizer.RecognizeLayoutAsync(imageBytes, OcrEngineKind.WindowsEnglishPreferred, cancellationToken);
    }

    private async Task<string?> RecognizeWithAutoAsync(byte[] imageBytes, CancellationToken cancellationToken)
    {
        var autoOrder = new[]
        {
            OcrEngineKind.WindowsKoreanPreferred,
            OcrEngineKind.Tesseract,
            OcrEngineKind.GoogleVisionOcr,
            OcrEngineKind.NaverClovaOcr,
            OcrEngineKind.WindowsEnglishPreferred,
        };

        foreach (var engineKind in autoOrder)
        {
            var result = await RecognizeWithEngineAsync(imageBytes, engineKind, cancellationToken);
            if (!string.IsNullOrWhiteSpace(result))
            {
                return result;
            }
        }

        return null;
    }

    private Task<string?> RecognizeWithEngineAsync(byte[] imageBytes, OcrEngineKind engineKind, CancellationToken cancellationToken)
    {
        return engineKind switch
        {
            OcrEngineKind.WindowsKoreanPreferred or OcrEngineKind.WindowsEnglishPreferred
                => _windowsRecognizer.RecognizeAsync(imageBytes, engineKind, cancellationToken),
            OcrEngineKind.Tesseract
                => _tesseractRecognizer.RecognizeAsync(imageBytes, cancellationToken),
            OcrEngineKind.GoogleVisionOcr
                => _googleVisionRecognizer.RecognizeAsync(imageBytes, cancellationToken),
            OcrEngineKind.NaverClovaOcr
                => _naverClovaRecognizer.RecognizeAsync(imageBytes, cancellationToken),
            _ => Task.FromResult<string?>(null),
        };
    }
}
