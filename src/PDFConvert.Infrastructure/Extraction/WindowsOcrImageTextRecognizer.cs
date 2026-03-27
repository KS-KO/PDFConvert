using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using PDFConvert.Domain.Enums;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace PDFConvert.Infrastructure.Extraction;

internal sealed class WindowsOcrImageTextRecognizer
{
    private readonly IReadOnlyDictionary<OcrEngineKind, Lazy<OcrEngine?>> _ocrEngines;

    public WindowsOcrImageTextRecognizer()
    {
        _ocrEngines = new Dictionary<OcrEngineKind, Lazy<OcrEngine?>>
        {
            [OcrEngineKind.Auto] = new(() => CreateOcrEngine(OcrEngineKind.Auto)),
            [OcrEngineKind.WindowsKoreanPreferred] = new(() => CreateOcrEngine(OcrEngineKind.WindowsKoreanPreferred)),
            [OcrEngineKind.WindowsEnglishPreferred] = new(() => CreateOcrEngine(OcrEngineKind.WindowsEnglishPreferred)),
        };
    }

    public bool IsAvailable(OcrEngineKind engineKind) => GetEngine(engineKind) is not null;

    public async Task<string?> RecognizeAsync(byte[] imageBytes, OcrEngineKind engineKind, CancellationToken cancellationToken = default)
    {
        var layout = await RecognizeLayoutAsync(imageBytes, engineKind, cancellationToken);
        return string.IsNullOrWhiteSpace(layout?.Text) ? null : layout.Text;
    }

    public async Task<OcrPageLayout?> RecognizeLayoutAsync(byte[] imageBytes, OcrEngineKind engineKind, CancellationToken cancellationToken = default)
    {
        var engine = GetEngine(engineKind);
        if (imageBytes == null || imageBytes.Length == 0 || engine is null) return null;

        try
        {
            using var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(imageBytes.AsBuffer());
            stream.Seek(0);

            var decoder = await BitmapDecoder.CreateAsync(stream);
            using var bitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

            cancellationToken.ThrowIfCancellationRequested();

            var result = await engine.RecognizeAsync(bitmap);
            if (result == null || string.IsNullOrWhiteSpace(result.Text)) return null;

            var lines = result.Lines
                .Select(BuildLine)
                .Where(line => line is not null)
                .Cast<OcrTextLine>()
                .ToArray();

            return new OcrPageLayout
            {
                Text = result.Text,
                PixelWidth = bitmap.PixelWidth,
                PixelHeight = bitmap.PixelHeight,
                Lines = lines,
            };
        }
        catch { return null; }
    }

    private OcrEngine? GetEngine(OcrEngineKind engineKind)
    {
        if (_ocrEngines.TryGetValue(engineKind, out var engine) && engine.Value != null) return engine.Value;
        return _ocrEngines[OcrEngineKind.Auto].Value;
    }

    private static OcrEngine? CreateOcrEngine(OcrEngineKind engineKind)
    {
        try
        {
            var langTags = engineKind switch
            {
                OcrEngineKind.WindowsKoreanPreferred => new[] { "ko-KR", "en-US" },
                OcrEngineKind.WindowsEnglishPreferred => new[] { "en-US", "ko-KR" },
                _ => new[] { "ko-KR", "en-US" }
            };

            foreach (var tag in langTags)
            {
                var lang = new Language(tag);
                if (OcrEngine.IsLanguageSupported(lang)) return OcrEngine.TryCreateFromLanguage(lang);
            }

            // Final fallbacks
            return OcrEngine.TryCreateFromUserProfileLanguages() 
                   ?? OcrEngine.AvailableRecognizerLanguages.Select(OcrEngine.TryCreateFromLanguage).FirstOrDefault(e => e != null);
        }
        catch { return null; }
    }

    private static OcrTextLine? BuildLine(OcrLine line)
    {
        var words = line.Words.Where(w => !string.IsNullOrWhiteSpace(w.Text)).ToArray();
        if (words.Length == 0) return null;

        var left = words.Min(w => w.BoundingRect.X);
        var top = words.Min(w => w.BoundingRect.Y);
        var right = words.Max(w => w.BoundingRect.X + w.BoundingRect.Width);
        var bottom = words.Max(w => w.BoundingRect.Y + w.BoundingRect.Height);

        return new OcrTextLine
        {
            Text = string.Join(" ", words.Select(w => w.Text.Trim())),
            Left = left, Top = top, Width = Math.Max(1, right - left), Height = Math.Max(1, bottom - top)
        };
    }
}
