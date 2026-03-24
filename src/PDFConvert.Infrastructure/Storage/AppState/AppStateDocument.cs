using PDFConvert.Domain.Entities;

namespace PDFConvert.Infrastructure.Storage.AppState;

internal sealed class AppStateDocument
{
    public string? LastSelectedPdfPath { get; set; }
    public string? LastSelectedOutputDirectory { get; set; }
    public string? GoogleVisionApiKey { get; set; }
    public string? NaverClovaEndpoint { get; set; }
    public string? NaverClovaSecret { get; set; }
    public List<RecentConversionItem> RecentConversions { get; set; } = [];
}
