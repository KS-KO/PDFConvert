namespace PDFConvert.Domain.Entities;

public sealed class ConversionResult
{
    public bool IsSuccess { get; init; }
    public string SummaryMessage { get; init; } = string.Empty;
    public IReadOnlyList<string> OutputArtifacts { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
