using PDFConvert.Domain.Entities;
using PDFConvert.Domain.Enums;

namespace PDFConvert.Application.Interfaces;

public interface IPdfTextExtractor
{
    Task<PdfDocumentContent> ExtractAsync(
        string pdfPath,
        bool useOcr = false,
        OcrEngineKind ocrEngineKind = OcrEngineKind.Auto,
        CancellationToken cancellationToken = default);
}
