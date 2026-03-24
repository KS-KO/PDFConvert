namespace PDFConvert.Infrastructure.Extraction;

internal sealed class OcrPageLayout
{
    public string Text { get; init; } = string.Empty;

    public int PixelWidth { get; init; }

    public int PixelHeight { get; init; }

    public IReadOnlyList<OcrTextLine> Lines { get; init; } = [];
}

internal sealed class OcrTextLine
{
    public string Text { get; init; } = string.Empty;

    public double Left { get; init; }

    public double Top { get; init; }

    public double Width { get; init; }

    public double Height { get; init; }
}
