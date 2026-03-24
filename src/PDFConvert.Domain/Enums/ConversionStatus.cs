namespace PDFConvert.Domain.Enums;

public enum ConversionStatus
{
    Idle,
    Validating,
    Analyzing,
    Converting,
    Completed,
    Failed,
    Cancelled,
}
