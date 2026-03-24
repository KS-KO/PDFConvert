using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PDFConvert.Application.Interfaces;
using PDFConvert.Domain.Entities;
using PDFConvert.Domain.Enums;

namespace PDFConvert.Infrastructure.Writers;

public sealed class DocxOutputWriter : IOutputWriter
{
    public OutputFormat Format => OutputFormat.Docx;

    public Task<string> WriteAsync(PdfDocumentContent document, string outputDirectory, CancellationToken cancellationToken = default)
    {
        var outputPath = Path.Combine(outputDirectory, $"{Path.GetFileNameWithoutExtension(document.SourceFilePath)}.docx");

        using var wordDocument = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document);
        var mainPart = wordDocument.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());

        var body = mainPart.Document.Body!;

        foreach (var page in document.Pages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var formattedPage = DocumentTextFormatter.Format(page);

            body.Append(CreateHeadingParagraph(formattedPage.Title));

            foreach (var block in formattedPage.Blocks)
            {
                if (string.IsNullOrWhiteSpace(block.Text))
                {
                    continue;
                }

                body.Append(CreateBodyParagraph(block));
            }

            body.Append(new Paragraph(new Run(new Break { Type = BreakValues.Page })));
        }

        mainPart.Document.Save();
        return Task.FromResult(outputPath);
    }

    private static Paragraph CreateHeadingParagraph(string text)
    {
        return new Paragraph(
            new ParagraphProperties(new ParagraphStyleId { Val = "Heading1" }),
            new Run(new Text(text)));
    }

    private static Paragraph CreateBodyParagraph(DocumentTextFormatter.FormattedBlock block)
    {
        var paragraph = new Paragraph(
            new Run(new Text(block.Text) { Space = SpaceProcessingModeValues.Preserve }));

        if (!block.IsListItem)
        {
            return paragraph;
        }

        paragraph.ParagraphProperties = new ParagraphProperties(
            new NumberingProperties(
                new NumberingLevelReference { Val = 0 },
                new NumberingId { Val = 1 }));

        return paragraph;
    }
}
