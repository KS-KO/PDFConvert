namespace PDFConvert.Application.Interfaces;

public interface IOcrSettingsStore
{
    string? GetGoogleVisionApiKey();
    void SaveGoogleVisionApiKey(string? apiKey);

    string? GetGoogleClientId();
    void SaveGoogleClientId(string? clientId);

    string? GetGoogleClientSecret();
    void SaveGoogleClientSecret(string? clientSecret);

    string? GetNaverClovaEndpoint();
    void SaveNaverClovaEndpoint(string? endpoint);

    string? GetNaverClovaSecret();
    void SaveNaverClovaSecret(string? secret);
}
