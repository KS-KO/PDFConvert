namespace PDFConvert.Domain.Entities;

public sealed class PdfTextOverlay
{
    public string Text { get; init; } = string.Empty;

    public double LeftRatio { get; init; }

    public double TopRatio { get; init; }

    public double WidthRatio { get; init; }

    public double HeightRatio { get; init; }

    public string? FontColorHex { get; set; }
}
