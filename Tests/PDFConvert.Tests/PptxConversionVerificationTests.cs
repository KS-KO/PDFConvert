using PDFConvert.Application.Services;
using PDFConvert.Domain.Entities;
using PDFConvert.Domain.Enums;
using PDFConvert.Domain.ValueObjects;
using PDFConvert.Infrastructure.Extraction;
using PDFConvert.Infrastructure.Storage;
using PDFConvert.Infrastructure.Writers;

namespace PDFConvert.Tests;

public sealed class PptxConversionVerificationTests
{
    [Fact]
    public async Task ConvertAsync_Creates_Pptx_From_Sample_Pdf()
    {
        var repositoryRoot = ResolveRepositoryRoot();
        var sourcePdfPath = Path.Combine(repositoryRoot, "DOC", "LASER1205_Architecture_and_Safety.pdf");
        Assert.True(File.Exists(sourcePdfPath), $"Sample PDF not found: {sourcePdfPath}");

        var outputDirectory = Path.Combine(repositoryRoot, "DOC", "verification_output_pptx");
        if (Directory.Exists(outputDirectory))
        {
            Directory.Delete(outputDirectory, recursive: true);
        }

        Directory.CreateDirectory(outputDirectory);

        try
        {
            var coordinator = new ConversionCoordinator(
                new PdfPigTextExtractor(),
                new OutputPathResolver(),
                [new PptxOutputWriter()]);

            var result = await coordinator.ConvertAsync(
                new ConversionJob
                {
                    SourceFilePath = sourcePdfPath,
                    TargetFormats = [OutputFormat.Pptx],
                    OutputDirectory = outputDirectory,
                    UseOcr = true,
                    OcrEngineKind = OcrEngineKind.Auto,
                },
                new Progress<ConversionProgress>());

            var outputPath = Path.Combine(outputDirectory, "LASER1205_Architecture_and_Safety.pptx");

            Assert.True(result.IsSuccess);
            Assert.Single(result.OutputArtifacts);
            Assert.True(File.Exists(outputPath), $"PPTX output not found: {outputPath}");
            Assert.True(new FileInfo(outputPath).Length > 0, "PPTX output file is empty.");
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }

    private static string ResolveRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var docDirectory = Path.Combine(directory.FullName, "DOC");
            var srcDirectory = Path.Combine(directory.FullName, "src");
            if (Directory.Exists(docDirectory) && Directory.Exists(srcDirectory))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root could not be resolved.");
    }
}
