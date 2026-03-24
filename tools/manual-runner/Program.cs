using System;
using System.IO;
using PDFConvert.Application.Interfaces;
using PDFConvert.Application.Services;
using PDFConvert.Domain.Entities;
using PDFConvert.Domain.Enums;
using PDFConvert.Domain.ValueObjects;
using PDFConvert.Infrastructure.Extraction;
using PDFConvert.Infrastructure.Storage;
using PDFConvert.Infrastructure.Writers;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: ManualRunner <pdf-path>");
    return 1;
}

var sourcePdfPath = args[0];
var outputDirectory = Path.GetDirectoryName(sourcePdfPath);

if (string.IsNullOrWhiteSpace(outputDirectory))
{
    Console.Error.WriteLine("Output directory could not be resolved.");
    return 2;
}

var coordinator = new ConversionCoordinator(
    new PdfPigTextExtractor(),
    new OutputPathResolver(),
    [new PptxOutputWriter()]);

var progress = new Progress<ConversionProgress>(item =>
{
    Console.WriteLine($"{item.Percent}% {item.Message}");
});

var result = await coordinator.ConvertAsync(
    new ConversionJob
    {
        SourceFilePath = sourcePdfPath,
        TargetFormats = [OutputFormat.Pptx],
        OutputDirectory = outputDirectory,
        UseOcr = true,
        OcrEngineKind = OcrEngineKind.Auto,
    },
    progress);

Console.WriteLine(result.SummaryMessage);
foreach (var artifact in result.OutputArtifacts)
{
    Console.WriteLine(artifact);
}

return result.IsSuccess ? 0 : 3;
