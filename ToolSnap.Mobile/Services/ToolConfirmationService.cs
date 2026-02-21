using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ToolSnap.Mobile.Dtos;
using ToolSnap.Mobile.Models;

namespace ToolSnap.Mobile.Services;

public record ConfirmToolsResult(
    bool Success,
    string? ErrorMessage);

public class ToolConfirmationService
{
    private readonly HttpClient _httpClient;

    public ToolConfirmationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // =========================
    // 1. ДОВІДНИКИ
    // =========================
    public async Task<(List<ToolTypeDto> ToolTypes,
                       List<BrandDto> Brands,
                       List<ModelDto> Models)> LoadDictionariesAsync(
        CancellationToken cancellationToken = default)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // /tool-types
        var toolTypes = await _httpClient
            .GetFromJsonAsync<List<ToolTypeDto>>("tool-types", jsonOptions, cancellationToken)
            ?? new List<ToolTypeDto>();

        // /brands
        var brands = await _httpClient
            .GetFromJsonAsync<List<BrandDto>>("brands", jsonOptions, cancellationToken)
            ?? new List<BrandDto>();

        // /models
        var models = await _httpClient
            .GetFromJsonAsync<List<ModelDto>>("models", jsonOptions, cancellationToken)
            ?? new List<ModelDto>();

        return (toolTypes, brands, models);
    }

    // =========================
    // 2. ПОШУК ІНСТРУМЕНТІВ
    // =========================
    public async Task<List<ToolDto>> SearchNotReturnedToolsByUserAsync(
        Guid userId,
        Guid toolTypeId,
        Guid? brandId,
        Guid? modelId,
        CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder($"tools/not-returned/user/{userId}/search?toolTypeId=");
        sb.Append(toolTypeId.ToString());

        if (brandId.HasValue)
            sb.Append("&brandId=").Append(brandId.Value);

        if (modelId.HasValue)
            sb.Append("&modelId=").Append(modelId.Value);

        var url = sb.ToString();

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var response = await _httpClient.GetAsync(url, cancellationToken);
        var text = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // щоб сторінка не падала, а просто показала порожній список
            return new List<ToolDto>();
        }

        return JsonSerializer.Deserialize<List<ToolDto>>(text, jsonOptions)
               ?? new List<ToolDto>();
    }


    public async Task<List<ToolDto>> SearchToolsAsync(
        Guid toolTypeId,
        Guid? brandId,
        Guid? modelId,
        CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder("tools/search?toolTypeId=");
        sb.Append(toolTypeId.ToString());

        if (brandId.HasValue)
            sb.Append("&brandId=").Append(brandId.Value);

        if (modelId.HasValue)
            sb.Append("&modelId=").Append(modelId.Value);

        var url = sb.ToString();

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var response = await _httpClient.GetAsync(url, cancellationToken);
        var text = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // Не кидаємо — нехай сторінка покаже помилку
            return new List<ToolDto>();
        }

        return JsonSerializer.Deserialize<List<ToolDto>>(text, jsonOptions)
               ?? new List<ToolDto>();
    }

    // =========================
    // 3. CONFIRM ON Take (detected + assignments)
    // =========================
    public async Task<ConfirmToolsResult> ConfirmAsync(
        Guid userId,
        PhotoSessionDto photoSession,
        IList<ConfirmDetectedToolItem> items,
        CancellationToken cancellationToken)
    {
        if (items == null || items.Count == 0)
            return new ConfirmToolsResult(false, "No items to confirm.");

        try
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            // 1️⃣ Створюємо detected tools (detected-tools/batch)
            var detectedBatchBody = new CreateDetectedToolsBatchDto(
                 Items: items.Select(i => new CreateDetectedToolItemDto(
                 PhotoSessionId: i.PhotoSessionId,
                 ToolTypeId: i.SelectedToolType?.Id
                   ?? throw new InvalidOperationException("ToolType is required"),
                 BrandId: i.SelectedBrand?.Id,
                 ModelId: i.SelectedModel?.Id,
                 SerialNumber: i.SerialNumber,
                 Confidence: i.Confidence,
                 RedFlagged: i.RedFlagged
                )).ToList()
);


            var detectedResp = await _httpClient.PostAsJsonAsync(
                "detected-tools/batch",
                detectedBatchBody,
                cancellationToken);

            var detectedText = await detectedResp.Content.ReadAsStringAsync(cancellationToken);

            if (!detectedResp.IsSuccessStatusCode)
            {
                return new ConfirmToolsResult(
                    false,
                    $"CreateDetectedTools batch failed: {detectedResp.StatusCode}\n{detectedText}");
            }

            var detectedTools = JsonSerializer.Deserialize<List<DetectedToolDto>>(
                detectedText,
                jsonOptions) ?? new List<DetectedToolDto>();

            if (detectedTools.Count != items.Count)
            {
                return new ConfirmToolsResult(
                    false,
                    $"Detected tools count ({detectedTools.Count}) != items count ({items.Count}).");
            }

            // 2️⃣ Знаходимо найближчу локацію (locations/nearest)
            var lat = photoSession.Latitude.ToString(CultureInfo.InvariantCulture);
            var lng = photoSession.Longitude.ToString(CultureInfo.InvariantCulture);

            var nearestResp = await _httpClient.GetAsync(
                $"locations/nearest?latitude={lat}&longitude={lng}",
                cancellationToken);

            var nearestText = await nearestResp.Content.ReadAsStringAsync(cancellationToken);

            if (!nearestResp.IsSuccessStatusCode)
            {
                return new ConfirmToolsResult(
                    false,
                    $"Nearest location request failed: {nearestResp.StatusCode}\n{nearestText}");
            }

            var location = JsonSerializer.Deserialize<LocationDto>(
                nearestText,
                jsonOptions);

            if (location is null)
            {
                return new ConfirmToolsResult(false, "Failed to parse nearest LocationDto.");
            }

            var locationId = location.Id;

            // 3️⃣ Створюємо асайни (tool-assignments/batch)
            var assignmentItems = detectedTools
                .Zip(items, (det, item) => new CreateToolAssignmentsBatchItemDto(
                    TakenDetectedToolId: det.Id,
                    ToolId: item.SelectedTool?.Id
                            ?? throw new InvalidOperationException("Tool must be selected"),
                    UserId: userId,
                    LocationId: locationId
                ))
                .ToList();

            var assignmentsBody = new CreateToolAssignmentsBatchDto(assignmentItems);

            var assignmentsResp = await _httpClient.PostAsJsonAsync(
                "tool-assignments/batch",
                assignmentsBody,
                cancellationToken);

            var assignmentsText = await assignmentsResp.Content.ReadAsStringAsync(cancellationToken);

            if (!assignmentsResp.IsSuccessStatusCode)
            {
                return new ConfirmToolsResult(
                    false,
                    $"CreateToolAssignments batch failed: {assignmentsResp.StatusCode}\n{assignmentsText}");
            }

            return new ConfirmToolsResult(true, null);
        }
        catch (Exception ex)
        {
            return new ConfirmToolsResult(false, ex.Message);
        }
    }

    // =========================
    // 3. CONFIRM ON Return (detected + assignments)
    // =========================

public async Task<ConfirmToolsResult> ConfirmReturnAsync(
    Guid userId,
    PhotoSessionDto photoSession,
    IList<ConfirmDetectedToolItem> items,
    CancellationToken cancellationToken)
{
    try
    {
        // 🔥 DEBUG – вхід у метод
        await Application.Current.MainPage.DisplayAlertAsync(
            "DEBUG ConfirmReturn",
            $"userId: {userId}\nitems.Count: {items?.Count ?? 0}",
            "OK");

        if (items is null || items.Count == 0)
            return new ConfirmToolsResult(false, "No items to confirm.");

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // 1️⃣ Створюємо detected tools (detected-tools/batch)
        var detectedBatchBody = new CreateDetectedToolsBatchDto(
            Items: items.Select(i => new CreateDetectedToolItemDto(
                PhotoSessionId: i.PhotoSessionId,
                ToolTypeId: i.SelectedToolType?.Id
                    ?? throw new InvalidOperationException("ToolType is required"),
                BrandId: i.SelectedBrand?.Id,
                ModelId: i.SelectedModel?.Id,
                SerialNumber: i.SerialNumber,
                Confidence: i.Confidence,
                RedFlagged: i.RedFlagged
            )).ToList()
        );

        var detectedResp = await _httpClient.PostAsJsonAsync(
            "detected-tools/batch",
            detectedBatchBody,
            cancellationToken);

        var detectedText = await detectedResp.Content.ReadAsStringAsync(cancellationToken);

        if (!detectedResp.IsSuccessStatusCode)
        {
            return new ConfirmToolsResult(
                false,
                $"CreateDetectedTools batch failed: {detectedResp.StatusCode}\n{detectedText}");
        }

        var detectedTools = JsonSerializer.Deserialize<List<DetectedToolDto>>(
            detectedText,
            jsonOptions) ?? new List<DetectedToolDto>();

        if (detectedTools.Count != items.Count)
        {
            return new ConfirmToolsResult(
                false,
                $"Detected tools count ({detectedTools.Count}) != items count ({items.Count}).");
        }

        // 2️⃣ Знаходимо найближчу локацію
        var lat = photoSession.Latitude.ToString(CultureInfo.InvariantCulture);
        var lng = photoSession.Longitude.ToString(CultureInfo.InvariantCulture);

        var nearestResp = await _httpClient.GetAsync(
            $"locations/nearest?latitude={lat}&longitude={lng}",
            cancellationToken);

        var nearestText = await nearestResp.Content.ReadAsStringAsync(cancellationToken);

        if (!nearestResp.IsSuccessStatusCode)
        {
            return new ConfirmToolsResult(
                false,
                $"Nearest location request failed: {nearestResp.StatusCode}\n{nearestText}");
        }

        var location = JsonSerializer.Deserialize<LocationDto>(
            nearestText,
            jsonOptions);

        if (location is null)
        {
            return new ConfirmToolsResult(false, "Failed to parse nearest LocationDto.");
        }

        var locationId = location.Id;

        // 🔥 DEBUG – перед пошуком assignment-ів
        await Application.Current.MainPage.DisplayAlert(
            "DEBUG ConfirmReturn",
            $"DetectedTools: {detectedTools.Count}\nItems: {items.Count}\nLocationId: {locationId}",
            "OK");

        // 3️⃣ Для кожного item – шукаємо активний assignment
        var batchItems = new List<ReturnToolAssignmentsBatchItemDto>();

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var det = detectedTools[i];

            var toolId = item.SelectedTool?.Id
                ?? throw new InvalidOperationException("Tool must be selected.");

            var activeResp = await _httpClient.GetAsync(
                $"tool-assignments/user/{userId}/tool/{toolId}/search-active",
                cancellationToken);

            var activeText = await activeResp.Content.ReadAsStringAsync(cancellationToken);

            if (!activeResp.IsSuccessStatusCode)
            {
                return new ConfirmToolsResult(
                    false,
                    $"Active assignment request failed for tool {toolId}: {activeResp.StatusCode}\n{activeText}");
            }

            var assignment = JsonSerializer.Deserialize<ToolAssignmentDto>(
                activeText,
                jsonOptions);

            if (assignment is null)
            {
                return new ConfirmToolsResult(
                    false,
                    $"Failed to parse ToolAssignmentDto for tool {toolId}.");
            }

            // 🔥 DEBUG assignment
            await Application.Current.MainPage.DisplayAlert(
                "DEBUG ASSIGNMENT",
                $"Item index: {i}\n" +
                $"Selected ToolId: {toolId}\n\n" +
                $"AssignmentId: {assignment.Id}\n" +
                $"Assignment.ToolId: {assignment.ToolId}\n" +
                $"UserId: {assignment.UserId}\n" +
                $"TakenDetectedToolId: {assignment.TakenDetectedToolId}\n" +
                $"ReturnedDetectedToolId: {assignment.ReturnedDetectedToolId}\n" +
                $"TakenLocationId: {assignment.TakenLocationId}\n" +
                $"ReturnedLocationId: {assignment.ReturnedLocationId}\n" +
                $"TakenAt: {assignment.TakenAt}\n" +
                $"ReturnedAt: {assignment.ReturnedAt}",
                "OK");

            if (assignment.ReturnedAt.HasValue)
            {
                return new ConfirmToolsResult(
                    false,
                    $"Assignment {assignment.Id} for tool {toolId} is already returned.");
            }

            if (assignment.ToolId != toolId)
            {
                return new ConfirmToolsResult(
                    false,
                    $"Assignment.ToolId ({assignment.ToolId}) != selected ToolId ({toolId}).");
            }

            batchItems.Add(new ReturnToolAssignmentsBatchItemDto(
                ToolAssignmentId: assignment.Id,
                LocationId: locationId,
                ReturnedDetectedToolId: det.Id
            ));
        }

        // 4️⃣ Відправляємо batch/return
        var batchDto = new ReturnToolAssignmentsBatchDto(batchItems);

        var returnResp = await _httpClient.PostAsJsonAsync(
            "tool-assignments/batch/return",
            batchDto,
            cancellationToken);

        var returnText = await returnResp.Content.ReadAsStringAsync(cancellationToken);

        if (!returnResp.IsSuccessStatusCode)
        {
            return new ConfirmToolsResult(
                false,
                $"Return batch failed: {returnResp.StatusCode}\n{returnText}");
        }

        return new ConfirmToolsResult(true, null);
    }
    catch (Exception ex)
    {
        return new ConfirmToolsResult(false, ex.Message);
    }
}
}
