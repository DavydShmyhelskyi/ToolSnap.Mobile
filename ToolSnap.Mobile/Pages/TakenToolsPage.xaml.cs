using System.Collections.ObjectModel;
using System.Net.Http.Json;
using ToolSnap.Mobile.Dtos;
using ToolSnap.Mobile.Services; 

namespace ToolSnap.Mobile.Pages;

public partial class TakenToolsPage : ContentPage
{
    private readonly UserSessionService _session;
    private readonly HttpClient _httpClient;

    public ObservableCollection<ToolItemViewModel> Tools { get; } = new();

    public TakenToolsPage(UserSessionService session, HttpClient httpClient)
    {
        InitializeComponent();

        _session = session;
        _httpClient = httpClient;

        BindingContext = this;

        // фоновий старт загрузки
        _ = LoadToolsAsync();
    }

    private async Task LoadToolsAsync()
    {
        try
        {
            var user = _session.CurrentUser;
            if (user == null)
            {
                await DisplayAlertAsync("Error", "Not athorised.", "OK");
                return;
            }

            // 1️⃣ Завантажуємо всі типи інструментів
            var toolTypes = await _httpClient.GetFromJsonAsync<List<ToolTypeDto>>("tool-types");

            var typeDict = toolTypes?
                .ToDictionary(t => t.Id, t => t.Title)
                ?? new Dictionary<Guid, string>();

            // 2️⃣ Завантажуємо неповернуті інструменти
            var tools = await _httpClient.GetFromJsonAsync<List<ToolDto>>(
                $"tools/not-returned/user/{user.Id}");

            Tools.Clear();

            if (tools == null || tools.Count == 0)
                return;

            foreach (var tool in tools)
            {
                ImageSource? photo = null;

                // Тягнемо фото типу "front"
                try
                {
                    var resp = await _httpClient.GetAsync(
                        $"tool-photos/file?toolId={tool.Id}&photoTypeTitle=front");

                    if (resp.IsSuccessStatusCode)
                    {
                        var dto = await resp.Content.ReadFromJsonAsync<ToolPhotoFileDto>();
                        if (dto?.Content?.Length > 0)
                            photo = ImageSource.FromStream(() => new MemoryStream(dto.Content));
                    }
                }
                catch
                {
                    // якщо фото нема — ок
                }

                // 3️⃣ Отримуємо назву типу
                typeDict.TryGetValue(tool.ToolTypeId, out var typeTitle);
                typeTitle ??= "Невідомий тип";

                // 4️⃣ Додаємо елемент до списку
                Tools.Add(new ToolItemViewModel
                {
                    Id = tool.Id,
                    SerialNumber = tool.SerialNumber,
                    Photo = photo,
                    ToolTypeTitle = typeTitle
                });
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }
}

public class ToolItemViewModel
{
    public Guid Id { get; init; }
    public string? SerialNumber { get; init; }
    public ImageSource? Photo { get; init; }
    public string ToolTypeTitle { get; init; } = "";
}