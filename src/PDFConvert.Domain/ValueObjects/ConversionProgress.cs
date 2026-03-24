using PDFConvert.Domain.Enums;

namespace PDFConvert.Domain.ValueObjects;

public sealed record ConversionProgress(
    ConversionStatus Status,
    int Percent,
    string Message);
