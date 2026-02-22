using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ToolSnap.Mobile.Dtos;

namespace ToolSnap.Mobile.Services;

public class FindToolsForMapService
{
    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public FindToolsForMapService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    // =========================
    // 1. ДОВІДНИКИ
    // =========================

    public async Task<List<ToolTypeDto>> GetToolTypesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _httpClient
            .GetFromJsonAsync<List<ToolTypeDto>>("tool-types", JsonOptions, cancellationToken)
            ?? new List<ToolTypeDto>();
    }

    public async Task<List<BrandDto>> GetBrandsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _httpClient
            .GetFromJsonAsync<List<BrandDto>>("brands", JsonOptions, cancellationToken)
            ?? new List<BrandDto>();
    }

    public async Task<List<ModelDto>> GetModelsAsync(
        CancellationToken cancellationToken = default)
    {
        // Модель більше не використовується як фільтр,
        // але можемо її підтягувати для підписів на маркерах.
        return await _httpClient
            .GetFromJsonAsync<List<ModelDto>>("models", JsonOptions, cancellationToken)
            ?? new List<ModelDto>();
    }

    private async Task<(List<ToolTypeDto> ToolTypes,
                        List<BrandDto> Brands,
                        List<ModelDto> Models)> LoadDictionariesAsync(
        CancellationToken cancellationToken = default)
    {
        var ttTask = GetToolTypesAsync(cancellationToken);
        var brTask = GetBrandsAsync(cancellationToken);
        var mdTask = GetModelsAsync(cancellationToken);

        await Task.WhenAll(ttTask, brTask, mdTask);

        return (ttTask.Result, brTask.Result, mdTask.Result);
    }

    // =========================
    // 2. МАРКЕРИ ДЛЯ КАРТИ
    // =========================

    /// <summary>
    /// Основний метод, який викликає MapPage.
    /// modelId тепер ігноруємо.
    /// </summary>
    public async Task<IReadOnlyList<MapMarkerDto>> LoadMarkersAsync(
        ToolAvailabilityFilter availabilityFilter,
        Guid? toolTypeId,
        Guid? brandId,
        Guid? modelId,             // 🔸 спеціально лишаємо, просто не використовуємо
        Guid? currentUserId = null,
        CancellationToken cancellationToken = default)
    {
        // 1) тягнемо користувачів і локації
        var usersTask = _httpClient.GetFromJsonAsync<List<UserDto>>("users", JsonOptions, cancellationToken);
        var locationsTask = _httpClient.GetFromJsonAsync<List<LocationDto>>("locations", JsonOptions, cancellationToken);

        // 2) тулзи з урахуванням availability + фільтрів type / brand
        var tools = await LoadToolsByAvailabilityAsync(
            availabilityFilter,
            toolTypeId,
            brandId,
            currentUserId,
            cancellationToken);

        await Task.WhenAll(usersTask!, locationsTask!);

        var users = usersTask!.Result ?? new List<UserDto>();
        var locations = locationsTask!.Result ?? new List<LocationDto>();

        // 3) довідники для красивих підписів
        var (toolTypes, brands, models) = await LoadDictionariesAsync(cancellationToken);
        var toolTypeDict = toolTypes.ToDictionary(t => t.Id, t => t.Title);
        var brandDict = brands.ToDictionary(b => b.Id, b => b.Title);
        var modelDict = models.ToDictionary(m => m.Id, m => m.Title);

        var markers = new List<MapMarkerDto>();

        markers.AddRange(BuildUserMarkers(users));
        markers.AddRange(BuildLocationMarkers(locations));
        markers.AddRange(BuildToolMarkers(tools, locations, toolTypeDict, brandDict, modelDict));

        return markers;
    }

    // ----------------- тулзи під availability -----------------

    private async Task<List<ToolDto>> LoadToolsByAvailabilityAsync(
        ToolAvailabilityFilter availability,
        Guid? toolTypeId,
        Guid? brandId,
        Guid? currentUserId,
        CancellationToken cancellationToken)
    {
        switch (availability)
        {
            case ToolAvailabilityFilter.Available:
                // тут логічно використати твій /tools/search-available
                return await SearchAvailableToolsAsync(toolTypeId, brandId, cancellationToken);

            case ToolAvailabilityFilter.NotAvailable:
                // Повноцінно має працювати через /tools/not-returned/user/{userId}/search,
                // але поки currentUserId нам не передають → підстрахуємося fallback-ом.
                if (currentUserId is Guid uid)
                {
                    var notReturned = await SearchNotReturnedToolsForUserAsync(
                        uid, toolTypeId, brandId, cancellationToken);

                    if (notReturned.Count > 0)
                        return notReturned;
                }

                // fallback – просто будь-які тулзи, щоб не було порожньо
                return await SearchAnyToolsAsync(toolTypeId, brandId, cancellationToken);

            case ToolAvailabilityFilter.All:
            default:
                // головний сценарій – новий ендпоінт /tools/search-any
                return await SearchAnyToolsAsync(toolTypeId, brandId, cancellationToken);
        }
    }

    /// <summary>
    /// /tools/search-any?toolTypeId=...&brandId=...
    /// </summary>
    private async Task<List<ToolDto>> SearchAnyToolsAsync(
        Guid? toolTypeId,
        Guid? brandId,
        CancellationToken cancellationToken)
    {
        var sb = new StringBuilder("tools/search-any");

        var hasQuery = false;
        if (toolTypeId is Guid tt)
        {
            sb.Append(hasQuery ? '&' : '?');
            hasQuery = true;
            sb.Append("toolTypeId=").Append(tt);
        }

        if (brandId is Guid br)
        {
            sb.Append(hasQuery ? '&' : '?');
            hasQuery = true;
            sb.Append("brandId=").Append(br);
        }

        var response = await _httpClient.GetAsync(sb.ToString(), cancellationToken);
        var text = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // можна залогувати text, але для мапи просто повертаємо пустий список
            return new List<ToolDto>();
        }

        return JsonSerializer.Deserialize<List<ToolDto>>(text, JsonOptions)
               ?? new List<ToolDto>();
    }

    /// <summary>
    /// /tools/search-available?toolTypeId=...&brandId=...
    /// (як у твоєму ToolConfirmationService)
    /// </summary>
    private async Task<List<ToolDto>> SearchAvailableToolsAsync(
        Guid? toolTypeId,
        Guid? brandId,
        CancellationToken cancellationToken)
    {
        // для search-available ти в API вимагав toolTypeId – без нього не шукаємо
        if (toolTypeId is null)
            return new List<ToolDto>();

        var sb = new StringBuilder("tools/search-available?toolTypeId=");
        sb.Append(toolTypeId.Value);

        if (brandId is Guid br)
            sb.Append("&brandId=").Append(br);

        var response = await _httpClient.GetAsync(sb.ToString(), cancellationToken);
        var text = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            return new List<ToolDto>();

        return JsonSerializer.Deserialize<List<ToolDto>>(text, JsonOptions)
               ?? new List<ToolDto>();
    }

    /// <summary>
    /// /tools/not-returned/user/{userId}/search?toolTypeId=...&brandId=...
    /// Повністю запрацює, коли ми почнемо передавати currentUserId з MapPage.
    /// </summary>
    private async Task<List<ToolDto>> SearchNotReturnedToolsForUserAsync(
        Guid userId,
        Guid? toolTypeId,
        Guid? brandId,
        CancellationToken cancellationToken)
    {
        if (toolTypeId is null)
            return new List<ToolDto>();

        var sb = new StringBuilder($"tools/not-returned/user/{userId}/search?toolTypeId=");
        sb.Append(toolTypeId.Value);

        if (brandId is Guid br)
            sb.Append("&brandId=").Append(br);

        var response = await _httpClient.GetAsync(sb.ToString(), cancellationToken);
        var text = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            return new List<ToolDto>();

        return JsonSerializer.Deserialize<List<ToolDto>>(text, JsonOptions)
               ?? new List<ToolDto>();
    }

    // ----------------- мітки -----------------

    private static IReadOnlyList<MapMarkerDto> BuildUserMarkers(IEnumerable<UserDto> users)
    {
        var result = new List<MapMarkerDto>();

        foreach (var u in users)
        {
            if (u.Latitude is null || u.Longitude is null)
                continue;

            result.Add(new MapMarkerDto(
                Id: u.Id.ToString(),
                Kind: MapMarkerKind.User,
                Latitude: u.Latitude.Value,
                Longitude: u.Longitude.Value,
                Title: u.FullName,
                Subtitle: "User",
                Icon: "user"
            ));
        }

        return result;
    }

    private static IReadOnlyList<MapMarkerDto> BuildLocationMarkers(IEnumerable<LocationDto> locations)
    {
        var result = new List<MapMarkerDto>();

        foreach (var l in locations)
        {
            result.Add(new MapMarkerDto(
                Id: l.Id.ToString(),
                Kind: MapMarkerKind.Location,
                Latitude: l.Latitude,
                Longitude: l.Longitude,
                Title: l.Name,
                Subtitle: l.Address ?? "Location",
                Icon: "location"
            ));
        }

        return result;
    }

    private static IReadOnlyList<MapMarkerDto> BuildToolMarkers(
        IEnumerable<ToolDto> tools,
        IEnumerable<LocationDto> locations,
        IDictionary<Guid, string> toolTypes,
        IDictionary<Guid, string> brands,
        IDictionary<Guid, string> models)
    {
        var result = new List<MapMarkerDto>();

        // поки що в тебе немає координат у ToolDto,
        // тому прив'язуємо всі тулзи до якоїсь базової локації (наприклад, першої)
        var defaultLocation = locations.FirstOrDefault();
        if (defaultLocation == null)
            return result;

        foreach (var t in tools)
        {
            var typeTitle = toolTypes.TryGetValue(t.ToolTypeId, out var tt) ? tt : "Tool";
            var brandTitle = t.BrandId.HasValue && brands.TryGetValue(t.BrandId.Value, out var bt)
                ? bt
                : "Brand";

            string modelTitle = "";
            if (t.ModelId.HasValue && models.TryGetValue(t.ModelId.Value, out var mt))
                modelTitle = mt;

            var title = $"{typeTitle} ({brandTitle})";

            var subtitleParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(modelTitle))
                subtitleParts.Add(modelTitle);
            subtitleParts.Add($"SN: {t.SerialNumber ?? "-"}");
            subtitleParts.Add($"Kind: {MapMarkerKind.Tool}");

            var subtitle = string.Join(" • ", subtitleParts);

            result.Add(new MapMarkerDto(
                Id: t.Id.ToString(),
                Kind: MapMarkerKind.Tool,
                Latitude: defaultLocation.Latitude,
                Longitude: defaultLocation.Longitude,
                Title: title,
                Subtitle: subtitle,
                Icon: "tool"
            ));
        }

        return result;
    }
}