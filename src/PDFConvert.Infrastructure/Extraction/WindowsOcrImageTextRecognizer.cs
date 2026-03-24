using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using PDFConvert.Domain.Enums;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
using Windows.System.UserProfile;

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

    public async Task<string?> RecognizeAsync(
        byte[] imageBytes,
        OcrEngineKind engineKind,
        CancellationToken cancellationToken = default)
    {
        var layout = await RecognizeLayoutAsync(imageBytes, engineKind, cancellationToken);
        return string.IsNullOrWhiteSpace(layout?.Text) ? null : layout.Text;
    }

    public async Task<OcrPageLayout?> RecognizeLayoutAsync(
        byte[] imageBytes,
        OcrEngineKind engineKind,
        CancellationToken cancellationToken = default)
    {
        var engine = GetEngine(engineKind);
        if (imageBytes.Length == 0 || engine is null)
        {
            return null;
        }

        try
        {
            await using var stream = new MemoryStream(imageBytes, writable: false);
            using var randomAccessStream = stream.AsRandomAccessStream();
            var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
            var bitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);

            cancellationToken.ThrowIfCancellationRequested();

            var result = await engine.RecognizeAsync(bitmap);
            var text = result?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var lines = result!.Lines
                .Select(BuildLine)
                .Where(line => line is not null)
                .Cast<OcrTextLine>()
                .ToArray();

            return new OcrPageLayout
            {
                Text = text,
                PixelWidth = bitmap.PixelWidth,
                PixelHeight = bitmap.PixelHeight,
                Lines = lines,
            };
        }
        catch
        {
            return null;
        }
    }

    private OcrEngine? GetEngine(OcrEngineKind engineKind)
    {
        return _ocrEngines.TryGetValue(engineKind, out var engine)
            ? engine.Value
            : _ocrEngines[OcrEngineKind.Auto].Value;
    }

    private static OcrEngine? CreateOcrEngine(OcrEngineKind engineKind)
    {
        try
        {
            var prioritizedTags = engineKind switch
            {
                OcrEngineKind.WindowsKoreanPreferred => new List<string> { "ko-KR", "ko", "en-US", "en" },
                OcrEngineKind.WindowsEnglishPreferred => new List<string> { "en-US", "en", "ko-KR", "ko" },
                _ => new List<string> { "ko-KR", "en-US", "ko", "en" },
            };

            prioritizedTags.AddRange(GlobalizationPreferences.Languages);

            var preferredLanguages = prioritizedTags
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var tag in preferredLanguages)
            {
                var language = new Language(tag);
                if (OcrEngine.IsLanguageSupported(language))
                {
                    return OcrEngine.TryCreateFromLanguage(language);
                }
            }

            return OcrEngine.TryCreateFromUserProfileLanguages() ?? OcrEngine.TryCreateFromLanguage(new Language("en"));
        }
        catch
        {
            return null;
        }
    }

    private static OcrTextLine? BuildLine(OcrLine line)
    {
        var words = line.Words
            .Where(word => !string.IsNullOrWhiteSpace(word.Text))
            .ToArray();

        if (words.Length == 0)
        {
            return null;
        }

        var left = words.Min(word => word.BoundingRect.X);
        var top = words.Min(word => word.BoundingRect.Y);
        var right = words.Max(word => word.BoundingRect.X + word.BoundingRect.Width);
        var bottom = words.Max(word => word.BoundingRect.Y + word.BoundingRect.Height);

        return new OcrTextLine
        {
            Text = string.Join(" ", words.Select(word => word.Text.Trim())),
            Left = left,
            Top = top,
            Width = Math.Max(1, right - left),
            Height = Math.Max(1, bottom - top),
        };
    }
}
