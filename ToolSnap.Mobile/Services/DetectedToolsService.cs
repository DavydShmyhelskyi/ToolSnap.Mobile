using System.Net.Http.Json;
using System.Text.Json;
using ToolSnap.Mobile.Dtos;

namespace ToolSnap.Mobile.Services;

public class DetectedToolsService
{
    private readonly HttpClient _httpClient;
    private readonly DetectionParsingService _parsingService;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public DetectedToolsService(HttpClient httpClient, DetectionParsingService parsingService)
    {
        _httpClient = httpClient;
        _parsingService = parsingService;
    }

    public async Task<(bool Success, string? ErrorMessage, IReadOnlyList<DetectedToolDto>? Items)>
        CreateDetectedToolsForSessionAsync(
            Guid photoSessionId,
            string? detectionRawJson,
            CancellationToken cancellationToken = default)
    {
        // 1) Парсимо Gemini detections
        var geminiDetections = _parsingService.ParseDetections(detectionRawJson);

        if (geminiDetections.Count == 0)
        {
            return (false, "No detections to save.", null);
        }

        // 2) Підтягуємо словники з API
        // TODO: за потреби заміни маршрути "tool-types", "brands", "models" на свої
        var toolTypes = await GetListAsync<ToolTypeDto>("tool-types", cancellationToken);
        var brands = await GetListAsync<BrandDto>("brands", cancellationToken);
        var models = await GetListAsync<ModelDto>("models", cancellationToken);

        if (toolTypes.Count == 0)
        {
            return (false, "No tool types available on server.", null);
        }

        // 3) Мапимо GeminiDetection -> CreateDetectedToolItemDto
        var items = new List<CreateDetectedToolItemDto>();

        foreach (var detection in geminiDetections)
        {
            // шукаємо ToolTypeId по назві
            var toolType = toolTypes
                .FirstOrDefault(t =>
                    string.Equals(t.Title, detection.ToolType, StringComparison.OrdinalIgnoreCase));

            if (toolType is null)
            {
                // якщо не знайшли — просто скіпаємо цей detection (можеш замінити на помилку)
                continue;
            }

            Guid? brandId = null;
            if (!string.IsNullOrWhiteSpace(detection.Brand))
            {
                var brand = brands.FirstOrDefault(b =>
                    string.Equals(b.Title, detection.Brand, StringComparison.OrdinalIgnoreCase));
                brandId = brand?.Id;
            }

            Guid? modelId = null;
            if (!string.IsNullOrWhiteSpace(detection.Model))
            {
                var model = models.FirstOrDefault(m =>
                    string.Equals(m.Title, detection.Model, StringComparison.OrdinalIgnoreCase));
                modelId = model?.Id;
            }

            var item = new CreateDetectedToolItemDto(
                PhotoSessionId: photoSessionId,
                ToolTypeId: toolType.Id,
                BrandId: brandId,
                ModelId: modelId,
                SerialNumber: null, // поки Gemini не повертає
                Confidence: detection.Confidence,
                RedFlagged: detection.RedFlagged);

            items.Add(item);
        }

        if (items.Count == 0)
        {
            return (false, "No detections matched known tool types.", null);
        }

        var batchRequest = new CreateDetectedToolsBatchDto(items);

        // 4) Викликаємо /detected-tools/batch
        var response = await _httpClient.PostAsJsonAsync(
            "detected-tools/batch",
            batchRequest,
            cancellationToken);

        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return (false,
                $"CreateDetectedTools batch failed: {response.StatusCode}\n{responseText}",
                null);
        }

        // 5) Парсимо відповідь як список DetectedToolDto
        try
        {
            var detectedTools = JsonSerializer.Deserialize<List<DetectedToolDto>>(
                responseText,
                _jsonOptions) ?? new List<DetectedToolDto>();

            return (true, null, detectedTools);
        }
        catch (JsonException ex)
        {
            return (false, $"Failed to parse DetectedToolDto list: {ex.Message}", null);
        }
    }

    private async Task<List<T>> GetListAsync<T>(string url, CancellationToken cancellationToken)
    {
        var resp = await _httpClient.GetAsync(url, cancellationToken);
        var text = await resp.Content.ReadAsStringAsync(cancellationToken);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"GET {url} failed: {resp.StatusCode}\n{text}");

        return JsonSerializer.Deserialize<List<T>>(text, _jsonOptions)
               ?? new List<T>();
    }
}
