namespace PDFConvert.Domain.Entities;

public sealed class PdfImageOverlay
{
    public byte[] ImageBytes { get; init; } = [];
    public double LeftRatio { get; init; }
    public double TopRatio { get; init; }
    public double WidthRatio { get; init; }
    public double HeightRatio { get; init; }
}
