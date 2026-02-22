using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.IO;
using ToolSnap.Mobile.Dtos;
using ToolSnap.Mobile.Services;

namespace ToolSnap.Mobile.Pages;

public partial class TakenToolsPage : ContentPage
{
    private readonly UserSessionService _session;
    private readonly HttpClient _httpClient;

    public ObservableCollection<ToolItemViewModel> Tools { get; } = new();

    private bool _isLoaded;

    public TakenToolsPage(UserSessionService session, HttpClient httpClient)
    {
        InitializeComponent();

        _session = session;
        _httpClient = httpClient;

        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Щоб не вантажити кожен раз при поверненні на сторінку
        //if (_isLoaded)
         //   return;

        _isLoaded = true;
        await LoadToolsAsync();
    }

    private async Task LoadToolsAsync()
    {
        try
        {
            var user = _session.CurrentUser;
            if (user == null)
            {
                await DisplayAlertAsync("Error", "Not authorised.", "OK");
                return;
            }

            // 🔹 1. Завантажуємо всі типи інструментів
            var toolTypesResponse = await _httpClient.GetAsync("tool-types");
            var toolTypesText = await toolTypesResponse.Content.ReadAsStringAsync();

            if (!toolTypesResponse.IsSuccessStatusCode)
            {
                await DisplayAlertAsync(
                    "Error",
                    $"Failed to load tool types:\n{toolTypesResponse.StatusCode}\n{toolTypesText}",
                    "OK");
                return;
            }

            var toolTypes = System.Text.Json.JsonSerializer.Deserialize<List<ToolTypeDto>>(
                toolTypesText,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var typeDict = toolTypes?
                .ToDictionary(t => t.Id, t => t.Title)
                ?? new Dictionary<Guid, string>();

            // 🔹 2. Завантажуємо неповернуті інструменти користувача
            var toolsResponse = await _httpClient.GetAsync($"tools/not-returned/user/{user.Id}");
            var toolsText = await toolsResponse.Content.ReadAsStringAsync();

            if (!toolsResponse.IsSuccessStatusCode)
            {
                await DisplayAlertAsync(
                    "Error",
                    $"Failed to load tools:\n{toolsResponse.StatusCode}\n{toolsText}",
                    "OK");
                return;
            }

            var tools = System.Text.Json.JsonSerializer.Deserialize<List<ToolDto>>(
                toolsText,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Tools.Clear();

            if (tools == null || tools.Count == 0)
                return;

            // 🔹 3. Для кожного інструменту — довантажуємо фото й додаємо до списку
            foreach (var tool in tools)
            {
                ImageSource? photo = null;

                try
                {
                    // GET /tool-photos/file?toolId={toolId}&photoTypeTitle=front
                    // ⚠️ Переконайся, що в БД реально існує тип фото "front"
                    var resp = await _httpClient.GetAsync(
                        $"tool-photos/file?toolId={tool.Id}&photoTypeTitle=front");

                    if (resp.IsSuccessStatusCode)
                    {
                        var dto = await resp.Content.ReadFromJsonAsync<ToolPhotoFileDto>(
                            new System.Text.Json.JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                        if (dto?.Content != null && dto.Content.Length > 0)
                        {
                            photo = ImageSource.FromStream(
                                () => new MemoryStream(dto.Content));
                        }
                    }
                    else
                    {
                        // Якщо треба задебажити:
                        // var txt = await resp.Content.ReadAsStringAsync();
                        // await DisplayAlert("Photo error", $"{resp.StatusCode}\n{txt}", "OK");
                    }
                }
                catch
                {
                    // Якщо фото немає / впала помилка — просто пропускаємо
                }

                typeDict.TryGetValue(tool.ToolTypeId, out var typeTitle);
                typeTitle ??= "Невідомий тип";

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
            await DisplayAlertAsync("Error", ex.ToString(), "OK");
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