using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Maui.Media;
using ToolSnap.Mobile.Dtos;

namespace ToolSnap.Mobile.Services;

public record TakeToolsResult(
    bool Success,
    string? ErrorMessage,
    PhotoSessionDto? Session,
    string? DetectionRawJson);

public class ToolTakeService
{
    private readonly HttpClient _httpClient;
    private readonly LocationService _locationService;

    public ToolTakeService(HttpClient httpClient, LocationService locationService)
    {
        _httpClient = httpClient;
        _locationService = locationService;
    }

    public async Task<TakeToolsResult> TakeToolsAsync(
        IReadOnlyList<FileResult> photos,
        CancellationToken cancellationToken = default)
    {
        if (photos == null || photos.Count == 0)
        {
            return new TakeToolsResult(false, "No photos provided.", null, null);
        }

        try
        {
            // 1️⃣ Геолокація (можеш прибрати, якщо не треба)
            double latitude = 0;
            double longitude = 0;

            try
            {
                var (lat, lng) = await _locationService.GetCurrentLocationAsync();
                latitude = lat;
                longitude = lng;
            }
            catch
            {
                // якщо не треба падати — просто залишаємо 0,0
            }

            // 2️⃣ ActionType "Take"
            // TODO: підставь свій реальний маршрут до контролера ActionType
            var actionTypeResponse = await _httpClient.GetAsync(
                "action-types/by-title/take",
                cancellationToken);

            var actionTypeText = await actionTypeResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!actionTypeResponse.IsSuccessStatusCode)
            {
                return new TakeToolsResult(
                    false,
                    $"ActionType request failed: {actionTypeResponse.StatusCode}\n{actionTypeText}",
                    null,
                    null);
            }

            var actionType = JsonSerializer.Deserialize<ActionTypeDto>(
                actionTypeText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (actionType is null)
            {
                return new TakeToolsResult(false, "Failed to parse ActionTypeDto.", null, null);
            }

            // 3️⃣ Створюємо PhotoSession
            var createSession = new CreatePhotoSessionDto(
                Latitude: latitude,
                Longitude: longitude,
                ActionTypeId: actionType.Id);

            // TODO: свій маршрут до PhotoSessionController.Create
            var sessionResponse = await _httpClient.PostAsJsonAsync(
                "photo-sessions",
                createSession,
                cancellationToken);

            var sessionText = await sessionResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!sessionResponse.IsSuccessStatusCode)
            {
                return new TakeToolsResult(
                    false,
                    $"Create session failed: {sessionResponse.StatusCode}\n{sessionText}",
                    null,
                    null);
            }

            var session = JsonSerializer.Deserialize<PhotoSessionDto>(
                sessionText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (session is null)
            {
                return new TakeToolsResult(false, "Failed to parse PhotoSessionDto.", null, null);
            }

            // 4️⃣ Завантаження всіх фото
            foreach (var photo in photos)
            {
                using var stream = await photo.OpenReadAsync();
                using var content = new MultipartFormDataContent();

                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

                // Імена полів мають збігатися з CreatePhotoForDetectionDto:
                // Guid PhotoSessionId, IFormFile File
                content.Add(new StringContent(session.Id.ToString()), "PhotoSessionId");
                content.Add(fileContent, "File", photo.FileName);

                // TODO: свій маршрут до CreatePhotoForDetection
                var uploadResponse = await _httpClient.PostAsync(
                    "photos-for-detection",
                    content,
                    cancellationToken);

                var uploadText = await uploadResponse.Content.ReadAsStringAsync(cancellationToken);

                if (!uploadResponse.IsSuccessStatusCode)
                {
                    return new TakeToolsResult(
                        false,
                        $"Upload failed: {uploadResponse.StatusCode}\n{uploadText}",
                        session,
                        null);
                }
            }

            // 5️⃣ Детект через Gemini (backend endpoint)
            // TODO: свій маршрут до DetectTools(Guid sessionId)
            var detectResponse = await _httpClient.PostAsync(
                $"photos-for-detection/detect/{session.Id}",
                content: null,
                cancellationToken);

            var detectText = await detectResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!detectResponse.IsSuccessStatusCode)
            {
                return new TakeToolsResult(
                    false,
                    $"Detection failed: {detectResponse.StatusCode}\n{detectText}",
                    session,
                    null);
            }

            // detectText = { detection = ... } – бек уже сходив у Gemini і щось зберіг у себе
            return new TakeToolsResult(
                true,
                null,
                session,
                detectText);
        }
        catch (Exception ex)
        {
            return new TakeToolsResult(false, ex.Message, null, null);
        }
    }
}
