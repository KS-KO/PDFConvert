using PDFConvert.Application.Interfaces;
using PDFConvert.Domain.Entities;
using PDFConvert.Domain.Enums;

namespace PDFConvert.Infrastructure.Writers;

public sealed class GoogleSlidesPlaceholderWriter : IOutputWriter
{
    public OutputFormat Format => OutputFormat.GoogleSlides;

    public Task<string> WriteAsync(PdfDocumentContent document, string outputDirectory, CancellationToken cancellationToken = default)
    {
        var outputPath = Path.Combine(outputDirectory, $"{Path.GetFileNameWithoutExtension(document.SourceFilePath)}_google_slides.txt");
        File.WriteAllText(
            outputPath,
            "Google Slides 직접 연동은 아직 구현되지 않았습니다. 생성된 PPTX를 Google Slides에 업로드하는 후속 작업이 필요합니다.");

        return Task.FromResult(outputPath);
    }
}
