namespace PDFConvert.Domain.Entities;

public sealed class RecentConversionItem
{
    public DateTimeOffset ConvertedAt { get; init; }
    public string SourceFileName { get; init; } = string.Empty;
    public string OutputDirectory { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
}
