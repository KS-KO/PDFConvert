using PDFConvert.Application.Interfaces;
using PDFConvert.Infrastructure.Storage.AppState;

namespace PDFConvert.Infrastructure.Storage;

public sealed class FileOcrSettingsStore : IOcrSettingsStore
{
    private readonly JsonAppStateStore _appStateStore;

    public FileOcrSettingsStore(JsonAppStateStore appStateStore)
    {
        _appStateStore = appStateStore;
    }

    public string? GetGoogleVisionApiKey() => _appStateStore.Load().GoogleVisionApiKey;

    public void SaveGoogleVisionApiKey(string? apiKey)
    {
        var document = _appStateStore.Load();
        document.GoogleVisionApiKey = Normalize(apiKey);
        _appStateStore.Save(document);
    }

    public string? GetGoogleClientId() => _appStateStore.Load().GoogleClientId;

    public void SaveGoogleClientId(string? clientId)
    {
        var document = _appStateStore.Load();
        document.GoogleClientId = Normalize(clientId);
        _appStateStore.Save(document);
    }

    public string? GetGoogleClientSecret() => _appStateStore.Load().GoogleClientSecret;

    public void SaveGoogleClientSecret(string? clientSecret)
    {
        var document = _appStateStore.Load();
        document.GoogleClientSecret = Normalize(clientSecret);
        _appStateStore.Save(document);
    }

    public string? GetNaverClovaEndpoint() => _appStateStore.Load().NaverClovaEndpoint;

    public void SaveNaverClovaEndpoint(string? endpoint)
    {
        var document = _appStateStore.Load();
        document.NaverClovaEndpoint = Normalize(endpoint);
        _appStateStore.Save(document);
    }

    public string? GetNaverClovaSecret() => _appStateStore.Load().NaverClovaSecret;

    public void SaveNaverClovaSecret(string? secret)
    {
        var document = _appStateStore.Load();
        document.NaverClovaSecret = Normalize(secret);
        _appStateStore.Save(document);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
