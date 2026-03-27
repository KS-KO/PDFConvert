using PDFConvert.Application.Interfaces;
using PDFConvert.Domain.Entities;
using PDFConvert.Domain.Enums;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace PDFConvert.Infrastructure.Extraction;

public sealed class PdfPigTextExtractor : IPdfTextExtractor
{
    private readonly CompositeOcrImageTextRecognizer _ocrRecognizer = new();

    public async Task<PdfDocumentContent> ExtractAsync(
        string pdfPath,
        bool useOcr = false,
        OcrEngineKind ocrEngineKind = OcrEngineKind.Auto,
        CancellationToken cancellationToken = default)
    {
        using var document = PdfDocument.Open(pdfPath);
        var pageRasterizer = await WindowsPdfPageRasterizer.CreateAsync(pdfPath, cancellationToken);

        var pages = new List<PdfTextPage>();
        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var renderedPage = pageRasterizer is null
                ? null
                : await pageRasterizer.RenderPageAsPngAsync(page.Number - 1, cancellationToken);

            var structured = ExtractStructuredText(page);
            var extractedText = structured.Text;
            var overlays = structured.Overlays.ToList();

            if (useOcr)
            {
                // 1. Extract from individual images (if any)
                var imageOnlyText = await TryExtractTextFromImagesAsync(page, ocrEngineKind, cancellationToken);
                extractedText = PdfPageTextMerger.Merge(structured.Text, imageOnlyText);

                // 2. Perform Full Page OCR to fill gaps (Hybrid)
                var renderedPageLayout = await TryExtractPageLayoutUsingOcrAsync(renderedPage, ocrEngineKind, cancellationToken);
                if (renderedPageLayout != null)
                {
                    var ocrOverlays = ToOverlays(renderedPageLayout);
                    // Filter out OCR results that overlap significantly with native text
                    foreach (var ocrOverlay in ocrOverlays)
                    {
                        if (!IsOverlappingWithAny(ocrOverlay, overlays))
                        {
                            overlays.Add(ocrOverlay);
                        }
                    }
                    extractedText = PdfPageTextMerger.Merge(extractedText, renderedPageLayout.Text);
                }
            }

            var extractedImages = ExtractImages(page);
            extractedText = OcrTextPostProcessor.Clean(extractedText);

            if (string.IsNullOrWhiteSpace(extractedText) &&
                renderedPage?.PngBytes is not { Length: > 0 })
            {
                continue;
            }

            pages.Add(new PdfTextPage
            {
                PageNumber = page.Number,
                Text = extractedText,
                PointsWidth = page.Width,
                PointsHeight = page.Height,
                RenderedPageImagePng = renderedPage?.PngBytes,
                RenderedPagePixelWidth = renderedPage?.PixelWidth ?? 0,
                RenderedPagePixelHeight = renderedPage?.PixelHeight ?? 0,
                TextOverlays = overlays,
                ImageOverlays = extractedImages,
            });
        }

        return new PdfDocumentContent
        {
            SourceFilePath = pdfPath,
            Pages = pages.ToArray(),
        };
    }

    private static bool ShouldTryRenderedPageOcr(string structuredText, string? imageOnlyText)
    {
        if (string.IsNullOrWhiteSpace(structuredText))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(imageOnlyText))
        {
            return ExtractedTextQualityEvaluator.Score(structuredText) < 0.45;
        }

        return false;
    }

    private async Task<OcrPageLayout?> TryExtractPageLayoutUsingOcrAsync(
        RenderedPdfPage? renderedPage,
        OcrEngineKind ocrEngineKind,
        CancellationToken cancellationToken)
    {
        if (renderedPage?.PngBytes is not { Length: > 0 })
        {
            return null;
        }

        return await _ocrRecognizer.RecognizeLayoutAsync(renderedPage.PngBytes, ocrEngineKind, cancellationToken);
    }

    private static bool IsOverlappingWithAny(PdfTextOverlay candidate, List<PdfTextOverlay> existing)
    {
        return existing.Any(e => 
        {
            var intersectionLeft = Math.Max(candidate.LeftRatio, e.LeftRatio);
            var intersectionTop = Math.Max(candidate.TopRatio, e.TopRatio);
            var intersectionRight = Math.Min(candidate.LeftRatio + candidate.WidthRatio, e.LeftRatio + e.WidthRatio);
            var intersectionBottom = Math.Min(candidate.TopRatio + candidate.HeightRatio, e.TopRatio + e.HeightRatio);

            if (intersectionRight > intersectionLeft && intersectionBottom > intersectionTop)
            {
                var intersectionArea = (intersectionRight - intersectionLeft) * (intersectionBottom - intersectionTop);
                var candidateArea = candidate.WidthRatio * candidate.HeightRatio;
                // If more than 50% of the candidate is covered by existing text, it's an overlap
                return intersectionArea / candidateArea > 0.5;
            }
            return false;
        });
    }

    private static string ChooseBestText(string extractedText, string? ocrText)
    {
        var normalizedExtractedText = Normalize(extractedText);
        var normalizedOcrText = Normalize(ocrText ?? string.Empty);

        if (string.IsNullOrWhiteSpace(normalizedExtractedText))
        {
            return normalizedOcrText;
        }

        if (string.IsNullOrWhiteSpace(normalizedOcrText))
        {
            return normalizedExtractedText;
        }

        var extractedScore = ExtractedTextQualityEvaluator.Score(normalizedExtractedText);
        var ocrScore = ExtractedTextQualityEvaluator.Score(normalizedOcrText);

        if (ocrScore >= extractedScore + 0.12)
        {
            return normalizedOcrText;
        }

        if (extractedScore < 0.45 && ocrScore > extractedScore)
        {
            return normalizedOcrText;
        }

        return normalizedExtractedText;
    }

    private async Task<string?> TryExtractTextFromImagesAsync(
        Page page,
        OcrEngineKind ocrEngineKind,
        CancellationToken cancellationToken)
    {
        if (!_ocrRecognizer.IsAvailable(ocrEngineKind) || page.NumberOfImages == 0)
        {
            return null;
        }

        var orderedImageTexts = new List<(double Top, string Text)>();

        foreach (var image in page.GetImages())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var imageBytes = TryGetImageBytes(image);
            if (imageBytes is null || imageBytes.Length == 0)
            {
                continue;
            }

            var text = await _ocrRecognizer.RecognizeAsync(imageBytes, ocrEngineKind, cancellationToken);
            if (!string.IsNullOrWhiteSpace(text))
            {
                orderedImageTexts.Add((image.Bounds.Top, OcrTextPostProcessor.Clean(text)));
            }
        }

        if (orderedImageTexts.Count == 0)
        {
            return null;
        }

        return Normalize(string.Join(Environment.NewLine + Environment.NewLine,
            orderedImageTexts.OrderByDescending(item => item.Top).Select(item => item.Text)));
    }

    private static IReadOnlyList<PdfImageOverlay> ExtractImages(Page page)
    {
        var result = new List<PdfImageOverlay>();
        foreach (var image in page.GetImages())
        {
            var bytes = TryGetImageBytes(image);
            if (bytes is { Length: > 0 })
            {
                result.Add(new PdfImageOverlay
                {
                    ImageBytes = bytes,
                    LeftRatio = ClampRatio(image.Bounds.Left / page.Width),
                    TopRatio = ClampRatio((page.Height - image.Bounds.Top) / page.Height),
                    WidthRatio = ClampRatio(image.Bounds.Width / page.Width),
                    HeightRatio = ClampRatio(image.Bounds.Height / page.Height),
                });
            }
        }
        return result;
    }

    private static IReadOnlyList<PdfTextOverlay> ToOverlays(OcrPageLayout layout)
    {
        if (layout.PixelWidth <= 0 || layout.PixelHeight <= 0)
        {
            return [];
        }

        return layout.Lines
            .Where(line => !string.IsNullOrWhiteSpace(line.Text))
            .Select(line => new PdfTextOverlay
            {
                Text = OcrTextPostProcessor.Clean(line.Text),
                LeftRatio = ClampRatio(line.Left / layout.PixelWidth),
                TopRatio = ClampRatio(line.Top / layout.PixelHeight),
                WidthRatio = ClampRatio(line.Width / layout.PixelWidth),
                HeightRatio = ClampRatio(line.Height / layout.PixelHeight),
            })
            .Where(overlay => !string.IsNullOrWhiteSpace(overlay.Text) && overlay.WidthRatio > 0 && overlay.HeightRatio > 0)
            .ToArray();
    }

    private static byte[]? TryGetImageBytes(IPdfImage image)
    {
        if (image.TryGetPng(out var pngBytes) && pngBytes is { Length: > 0 })
        {
            return pngBytes;
        }

        var rawBytes = image.RawBytes;
        if (rawBytes is not { Count: > 0 })
        {
            return null;
        }

        var bytes = rawBytes.ToArray();
        return LooksLikeEncodedImage(bytes) ? bytes : null;
    }

    private static bool LooksLikeEncodedImage(byte[] bytes)
    {
        return IsPng(bytes) || IsJpeg(bytes) || IsBmp(bytes) || IsGif(bytes) || IsTiff(bytes);
    }

    private static bool IsPng(byte[] bytes)
    {
        return bytes.Length >= 8 &&
               bytes[0] == 0x89 &&
               bytes[1] == 0x50 &&
               bytes[2] == 0x4E &&
               bytes[3] == 0x47;
    }

    private static bool IsJpeg(byte[] bytes)
    {
        return bytes.Length >= 3 &&
               bytes[0] == 0xFF &&
               bytes[1] == 0xD8 &&
               bytes[2] == 0xFF;
    }

    private static bool IsBmp(byte[] bytes)
    {
        return bytes.Length >= 2 &&
               bytes[0] == 0x42 &&
               bytes[1] == 0x4D;
    }

    private static bool IsGif(byte[] bytes)
    {
        return bytes.Length >= 4 &&
               bytes[0] == 0x47 &&
               bytes[1] == 0x49 &&
               bytes[2] == 0x46;
    }

    private static bool IsTiff(byte[] bytes)
    {
        return bytes.Length >= 4 &&
               ((bytes[0] == 0x49 && bytes[1] == 0x49 && bytes[2] == 0x2A && bytes[3] == 0x00) ||
                (bytes[0] == 0x4D && bytes[1] == 0x4D && bytes[2] == 0x00 && bytes[3] == 0x2A));
    }

    private static StructuredExtractionResult ExtractStructuredText(Page page)
    {
        var words = page.GetWords()
            .Where(word => word.TextOrientation == TextOrientation.Horizontal)
            .Where(word => !string.IsNullOrWhiteSpace(word.Text))
            .Select(word => new WordPosition(
                word.Text.Trim(),
                word.BoundingBox.Left,
                word.BoundingBox.Right,
                word.BoundingBox.Bottom,
                word.BoundingBox.Height))
            .OrderByDescending(word => word.BaselineY)
            .ThenBy(word => word.Left)
            .ToList();

        if (words.Count == 0)
        {
            return new StructuredExtractionResult
            {
                Text = Normalize(page.Text),
                Overlays = [],
            };
        }

        var lineTolerance = Math.Max(words.Average(word => word.Height) * 0.5, 2d);
        var lines = new List<LineBuffer>();

        foreach (var word in words)
        {
            var existingLine = lines.FirstOrDefault(line => Math.Abs(line.BaselineY - word.BaselineY) <= lineTolerance);
            if (existingLine is null)
            {
                existingLine = new LineBuffer(word.BaselineY);
                lines.Add(existingLine);
            }

            existingLine.Words.Add(word);
        }

        var orderedLines = lines
            .OrderByDescending(line => line.BaselineY)
            .ToList();

        var result = new List<string>();
        var overlays = new List<PdfTextOverlay>();
        var averageHeight = Math.Max(words.Average(word => word.Height), 1d);

        for (var i = 0; i < orderedLines.Count; i++)
        {
            var line = orderedLines[i];
            var segments = SplitIntoSegments(line.Words.OrderBy(word => word.Left).ToList(), averageHeight);
            foreach (var segment in segments)
            {
                var text = JoinWords(segment);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    result.Add(text);
                    overlays.Add(CreateStructuredOverlay(page, segment, text));
                }
            }

            if (i < orderedLines.Count - 1)
            {
                var gap = line.BaselineY - orderedLines[i + 1].BaselineY;
                if (gap > averageHeight * 1.6)
                {
                    result.Add(string.Empty);
                }
            }
        }

        return new StructuredExtractionResult
        {
            Text = Normalize(string.Join(Environment.NewLine, result)),
            Overlays = overlays,
        };
    }

    private static PdfTextOverlay CreateStructuredOverlay(Page page, IReadOnlyList<WordPosition> words, string text)
    {
        var left = words.Min(word => word.Left);
        var right = words.Max(word => word.Right);
        var top = words.Max(word => word.BaselineY + word.Height);
        var bottom = words.Min(word => word.BaselineY);

        return new PdfTextOverlay
        {
            Text = text,
            LeftRatio = ClampRatio(left / page.Width),
            TopRatio = ClampRatio((page.Height - top) / page.Height),
            WidthRatio = ClampRatio((right - left) / page.Width),
            HeightRatio = ClampRatio((top - bottom) / page.Height),
        };
    }

    private static double ClampRatio(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return 0;
        }

        return Math.Clamp(value, 0, 1);
    }

    private static IReadOnlyList<IReadOnlyList<WordPosition>> SplitIntoSegments(
        IReadOnlyList<WordPosition> words,
        double averageHeight)
    {
        if (words.Count == 0)
        {
            return [];
        }

        var segments = new List<List<WordPosition>>();
        var current = new List<WordPosition> { words[0] };
        var largeGapThreshold = Math.Max(averageHeight * 3.5, 24d);

        for (var i = 1; i < words.Count; i++)
        {
            var previous = words[i - 1];
            var word = words[i];
            var gap = word.Left - previous.Right;

            if (gap > largeGapThreshold)
            {
                segments.Add(current);
                current = new List<WordPosition>();
            }

            current.Add(word);
        }

        segments.Add(current);
        return segments;
    }

    private static string JoinWords(IReadOnlyList<WordPosition> words)
    {
        if (words.Count == 0)
        {
            return string.Empty;
        }

        var parts = new List<string> { words[0].Text };

        for (var i = 1; i < words.Count; i++)
        {
            var current = words[i].Text;
            if (IsRightAttachedPunctuation(current))
            {
                parts[^1] += current;
            }
            else
            {
                parts.Add(current);
            }
        }

        return string.Join(" ", parts);
    }

    private static bool IsRightAttachedPunctuation(string text)
    {
        return text is "," or "." or ":" or ";" or "!" or "?" or ")" or "]";
    }

    private static string Normalize(string text)
    {
        var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
        var lines = normalized.Split('\n');
        var result = new List<string>();
        var previousWasBlank = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            var isBlank = string.IsNullOrWhiteSpace(line);

            if (isBlank)
            {
                if (!previousWasBlank && result.Count > 0)
                {
                    result.Add(string.Empty);
                }

                previousWasBlank = true;
                continue;
            }

            result.Add(line);
            previousWasBlank = false;
        }

        while (result.Count > 0 && string.IsNullOrWhiteSpace(result[^1]))
        {
            result.RemoveAt(result.Count - 1);
        }

        return string.Join(Environment.NewLine, result);
    }

    private sealed record WordPosition(string Text, double Left, double Right, double BaselineY, double Height);

    private sealed class LineBuffer
    {
        public LineBuffer(double baselineY)
        {
            BaselineY = baselineY;
        }

        public double BaselineY { get; }

        public List<WordPosition> Words { get; } = [];
    }

    private sealed class StructuredExtractionResult
    {
        public string Text { get; init; } = string.Empty;

        public IReadOnlyList<PdfTextOverlay> Overlays { get; init; } = [];
    }
}
