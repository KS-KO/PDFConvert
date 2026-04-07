using PDFConvert.Application.Interfaces;
using PDFConvert.Domain.Entities;
using PDFConvert.Domain.Enums;
using PDFConvert.Domain.ValueObjects;

namespace PDFConvert.Application.Services;

public sealed class ConversionCoordinator : IConversionCoordinator
{
    private readonly IPdfTextExtractor _pdfTextExtractor;
    private readonly IOutputPathResolver _outputPathResolver;
    private readonly IReadOnlyDictionary<OutputFormat, IOutputWriter> _writers;

    public ConversionCoordinator(
        IPdfTextExtractor pdfTextExtractor,
        IOutputPathResolver outputPathResolver,
        IEnumerable<IOutputWriter> writers)
    {
        _pdfTextExtractor = pdfTextExtractor;
        _outputPathResolver = outputPathResolver;
        _writers = writers.ToDictionary(writer => writer.Format);
    }

    public async Task<ConversionResult> ConvertAsync(
        ConversionJob job,
        IProgress<ConversionProgress> progress,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(job.SourceFilePath))
        {
            throw new FileNotFoundException("선택한 파일을 찾을 수 없습니다.", job.SourceFilePath);
        }

        progress.Report(new ConversionProgress(ConversionStatus.Validating, 10, "파일을 확인하는 중입니다."));

        var outputDirectory = _outputPathResolver.GetOutputDirectory(job.SourceFilePath, job.OutputDirectory);
        Directory.CreateDirectory(outputDirectory);

        progress.Report(new ConversionProgress(ConversionStatus.Analyzing, 35, "파일에서 텍스트를 추출하는 중입니다."));
        var document = await _pdfTextExtractor.ExtractAsync(job.SourceFilePath, job.UseOcr, job.OcrEngineKind, cancellationToken);

        if (document.Pages.Count == 0)
        {
            throw new InvalidOperationException("파일에서 변환 가능한 텍스트를 찾지 못했습니다.");
        }

        progress.Report(new ConversionProgress(ConversionStatus.Converting, 60, "선택한 형식의 파일을 생성하는 중입니다."));

        var artifacts = new List<string>();
        var warnings = new List<string>();
        var total = job.TargetFormats.Count;

        for (var index = 0; index < total; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var format = job.TargetFormats.ElementAt(index);
            var percent = 60 + (int)Math.Round(((index + 1d) / total) * 35d);
            progress.Report(new ConversionProgress(ConversionStatus.Converting, percent, $"{format} 파일을 생성하는 중입니다."));

            if (_writers.TryGetValue(format, out var writer))
            {
                var outputPath = await writer.WriteAsync(document, outputDirectory, cancellationToken);
                artifacts.Add(outputPath);
            }
            else
            {
                warnings.Add($"{format} 형식은 아직 구현되지 않았습니다.");
            }
        }

        progress.Report(new ConversionProgress(ConversionStatus.Completed, 100, "변환이 완료되었습니다."));

        var summary = artifacts.Count > 0
            ? $"변환 완료: {artifacts.Count}개 파일이 생성되었습니다."
            : "변환 가능한 출력 형식이 없어 파일이 생성되지 않았습니다.";

        if (warnings.Count > 0)
        {
            summary = $"{summary} 경고 {warnings.Count}건이 있습니다.";
        }

        return new ConversionResult
        {
            IsSuccess = artifacts.Count > 0,
            SummaryMessage = summary,
            OutputArtifacts = artifacts,
            Warnings = warnings,
        };
    }
}
