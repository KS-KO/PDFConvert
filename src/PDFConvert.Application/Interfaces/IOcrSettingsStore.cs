namespace PDFConvert.Application.Interfaces;

public interface IOcrSettingsStore
{
    string? GetGoogleVisionApiKey();
    void SaveGoogleVisionApiKey(string? apiKey);

    string? GetNaverClovaEndpoint();
    void SaveNaverClovaEndpoint(string? endpoint);

    string? GetNaverClovaSecret();
    void SaveNaverClovaSecret(string? secret);
}
