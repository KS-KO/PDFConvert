using System.Net;
using System.Text;
using PDFConvert.Application.Interfaces;
using PDFConvert.Domain.Entities;
using PDFConvert.Domain.Enums;

namespace PDFConvert.Infrastructure.Writers;

public sealed class DocOutputWriter : IOutputWriter
{
    public OutputFormat Format => OutputFormat.Doc;

    public Task<string> WriteAsync(PdfDocumentContent document, string outputDirectory, CancellationToken cancellationToken = default)
    {
        var outputPath = Path.Combine(outputDirectory, $"{Path.GetFileNameWithoutExtension(document.SourceFilePath)}.doc");

        var builder = new StringBuilder();
        builder.AppendLine("<html><head><meta charset=\"utf-8\"></head><body>");

        foreach (var page in document.Pages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var formattedPage = DocumentTextFormatter.Format(page);
            builder.AppendLine($"<h1>{WebUtility.HtmlEncode(formattedPage.Title)}</h1>");

            foreach (var block in formattedPage.Blocks)
            {
                if (string.IsNullOrWhiteSpace(block.Text))
                {
                    continue;
                }

                if (block.IsListItem)
                {
                    builder.AppendLine($"<li>{WebUtility.HtmlEncode(block.Text)}</li>");
                }
                else
                {
                    builder.AppendLine($"<p>{WebUtility.HtmlEncode(block.Text)}</p>");
                }
            }
        }

        builder.AppendLine("</body></html>");
        File.WriteAllText(outputPath, builder.ToString(), Encoding.UTF8);
        return Task.FromResult(outputPath);
    }
}
