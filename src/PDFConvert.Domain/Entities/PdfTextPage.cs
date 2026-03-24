namespace PDFConvert.Domain.Entities;

public sealed class PdfTextPage
{
    public int PageNumber { get; init; }
    public string Text { get; init; } = string.Empty;
}
