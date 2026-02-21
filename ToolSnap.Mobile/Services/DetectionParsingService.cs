using System.Text.Json;
using ToolSnap.Mobile.Dtos;

namespace ToolSnap.Mobile.Services;

public class DetectionParsingService
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public IReadOnlyList<GeminiDetection> ParseDetections(string? rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
            return Array.Empty<GeminiDetection>();

        DetectToolsApiResponse? apiResponse;
        try
        {
            // 1) зовнішній JSON: { "detection": "...." }
            apiResponse = JsonSerializer.Deserialize<DetectToolsApiResponse>(
                rawJson,
                _jsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Failed to parse detect API response JSON.", ex);
        }

        if (apiResponse is null || string.IsNullOrWhiteSpace(apiResponse.Detection))
            return Array.Empty<GeminiDetection>();

        try
        {
            // 2) внутрішній JSON у полі detection: { "detections": [ ... ] }
            var envelope = JsonSerializer.Deserialize<GeminiDetectionEnvelope>(
                apiResponse.Detection,
                _jsonOptions);

            // 🔧 Ось тут була помилка типів
            return envelope?.Detections ?? new List<GeminiDetection>();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Failed to parse Gemini detection payload JSON.", ex);
        }
    }
}
