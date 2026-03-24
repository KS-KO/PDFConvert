using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace PDFConvert.Infrastructure.Extraction;

internal sealed class NaverClovaOcrImageTextRecognizer
{
    private static readonly HttpClient HttpClient = new();

    public bool IsAvailable()
    {
        return !string.IsNullOrWhiteSpace(GetEndpoint()) &&
               !string.IsNullOrWhiteSpace(GetSecret());
    }

    public async Task<string?> RecognizeAsync(byte[] imageBytes, CancellationToken cancellationToken = default)
    {
        var endpoint = GetEndpoint();
        var secret = GetSecret();

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(secret) || imageBytes.Length == 0)
        {
            return null;
        }

        try
        {
            var payload = new
            {
                version = "V2",
                requestId = Guid.NewGuid().ToString("N"),
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                lang = "ko",
                images = new[]
                {
                    new
                    {
                        format = "png",
                        name = "page",
                        data = Convert.ToBase64String(imageBytes),
                    },
                },
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Add("X-OCR-SECRET", secret);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var response = await HttpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

            if (json.RootElement.TryGetProperty("images", out var images) &&
                images.GetArrayLength() > 0)
            {
                var first = images[0];
                if (first.TryGetProperty("fields", out var fields))
                {
                    var parts = new List<string>();
                    foreach (var field in fields.EnumerateArray())
                    {
                        if (field.TryGetProperty("inferText", out var inferText))
                        {
                            var value = inferText.GetString();
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                parts.Add(value!);
                            }
                        }
                    }

                    if (parts.Count > 0)
                    {
                        return string.Join(' ', parts);
                    }
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private static string? GetEndpoint()
    {
        return Environment.GetEnvironmentVariable("NAVER_CLOVA_OCR_ENDPOINT");
    }

    private static string? GetSecret()
    {
        return Environment.GetEnvironmentVariable("NAVER_CLOVA_OCR_SECRET");
    }
}
