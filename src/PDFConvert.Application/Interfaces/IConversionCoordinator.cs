using PDFConvert.Domain.Entities;
using PDFConvert.Domain.ValueObjects;

namespace PDFConvert.Application.Interfaces;

public interface IConversionCoordinator
{
    Task<ConversionResult> ConvertAsync(
        ConversionJob job,
        IProgress<ConversionProgress> progress,
        CancellationToken cancellationToken = default);
}
