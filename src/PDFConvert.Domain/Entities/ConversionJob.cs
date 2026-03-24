using PDFConvert.Domain.Enums;

namespace PDFConvert.Domain.Entities;

public sealed class ConversionJob
{
    public Guid JobId { get; init; } = Guid.NewGuid();
    public required string SourceFilePath { get; init; }
    public required IReadOnlyCollection<OutputFormat> TargetFormats { get; init; }
    public string? OutputDirectory { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public bool UseOcr { get; init; }
    public OcrEngineKind OcrEngineKind { get; init; } = OcrEngineKind.Auto;
}
