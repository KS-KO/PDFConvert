using PDFConvert.Domain.Entities;

namespace PDFConvert.Application.Interfaces;

public interface IRecentConversionStore
{
    IReadOnlyList<RecentConversionItem> LoadRecentConversions();
    void SaveRecentConversions(IReadOnlyList<RecentConversionItem> items);
}
