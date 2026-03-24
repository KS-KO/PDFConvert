using PDFConvert.Domain.Entities;
using PDFConvert.Domain.Enums;

namespace PDFConvert.Application.Interfaces;

public interface IOutputWriter
{
    OutputFormat Format { get; }

    Task<string> WriteAsync(
        PdfDocumentContent document,
        string outputDirectory,
        CancellationToken cancellationToken = default);
}
