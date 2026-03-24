using PDFConvert.Application.Interfaces;
using PDFConvert.Domain.Entities;
using PDFConvert.Infrastructure.Storage.AppState;

namespace PDFConvert.Infrastructure.Storage;

public sealed class FileRecentConversionStore : IRecentConversionStore
{
    private readonly JsonAppStateStore _stateStore;

    public FileRecentConversionStore(JsonAppStateStore stateStore)
    {
        _stateStore = stateStore;
    }

    public IReadOnlyList<RecentConversionItem> LoadRecentConversions()
    {
        return _stateStore.Load().RecentConversions;
    }

    public void SaveRecentConversions(IReadOnlyList<RecentConversionItem> items)
    {
        var state = _stateStore.Load();
        state.RecentConversions = items.ToList();
        _stateStore.Save(state);
    }
}
