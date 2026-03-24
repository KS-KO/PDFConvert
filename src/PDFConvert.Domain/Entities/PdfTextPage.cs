namespace PDFConvert.Domain.Entities;

public sealed class PdfTextPage
{
    public int PageNumber { get; init; }
    public string Text { get; init; } = string.Empty;
    public byte[]? RenderedPageImagePng { get; init; }
    public int RenderedPagePixelWidth { get; init; }
    public int RenderedPagePixelHeight { get; init; }
    public IReadOnlyList<PdfTextOverlay> TextOverlays { get; init; } = [];
}
