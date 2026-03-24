using System.Net.Http.Json;
using System.Text.Json;

namespace PDFConvert.Infrastructure.Extraction;

internal sealed class GoogleVisionOcrImageTextRecognizer
{
    private static readonly HttpClient HttpClient = new();

    public bool IsAvailable() => !string.IsNullOrWhiteSpace(GetApiKey());

    public async Task<string?> RecognizeAsync(byte[] imageBytes, CancellationToken cancellationToken = default)
    {
        var apiKey = GetApiKey();
        if (string.IsNullOrWhiteSpace(apiKey) || imageBytes.Length == 0)
        {
            return null;
        }

        try
        {
            var requestBody = new
            {
                requests = new[]
                {
                    new
                    {
                        image = new
                        {
                            content = Convert.ToBase64String(imageBytes),
                        },
                        features = new[]
                        {
                            new
                            {
                                type = "TEXT_DETECTION",
                            },
                        },
                    },
                },
            };

            using var response = await HttpClient.PostAsJsonAsync(
                $"https://vision.googleapis.com/v1/images:annotate?key={Uri.EscapeDataString(apiKey)}",
                requestBody,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

            if (json.RootElement.TryGetProperty("responses", out var responses) &&
                responses.GetArrayLength() > 0)
            {
                var first = responses[0];
                if (first.TryGetProperty("fullTextAnnotation", out var fullTextAnnotation) &&
                    fullTextAnnotation.TryGetProperty("text", out var fullText))
                {
                    return fullText.GetString();
                }

                if (first.TryGetProperty("textAnnotations", out var textAnnotations) &&
                    textAnnotations.GetArrayLength() > 0 &&
                    textAnnotations[0].TryGetProperty("description", out var description))
                {
                    return description.GetString();
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private static string? GetApiKey()
    {
        return Environment.GetEnvironmentVariable("GOOGLE_VISION_API_KEY");
    }
}
