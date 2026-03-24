namespace PDFConvert.Domain.Entities;

public sealed class PdfDocumentContent
{
    public string SourceFilePath { get; init; } = string.Empty;
    public IReadOnlyList<PdfTextPage> Pages { get; init; } = [];
}
