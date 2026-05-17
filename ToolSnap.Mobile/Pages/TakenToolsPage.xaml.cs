using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.IO;
using ToolSnap.Mobile.Dtos;
using ToolSnap.Mobile.Models;
using ToolSnap.Mobile.Services;

namespace ToolSnap.Mobile.Pages;

public partial class TakenToolsPage : ContentPage
{
    private readonly UserSessionService _session;
    private readonly HttpClient _httpClient;
    private readonly TransferFlowStateService _transferState;

    public ObservableCollection<ToolItemViewModel> Tools { get; } = new();

    private string _totalLiabilityText = "—";
    public string TotalLiabilityText
    {
        get => _totalLiabilityText;
        private set { _totalLiabilityText = value; OnPropertyChanged(nameof(TotalLiabilityText)); }
    }

    private string _toolCountText = "";
    public string ToolCountText
    {
        get => _toolCountText;
        private set { _toolCountText = value; OnPropertyChanged(nameof(ToolCountText)); }
    }

    public TakenToolsPage(
        UserSessionService session,
        HttpClient httpClient,
        TransferFlowStateService transferState)
    {
        InitializeComponent();

        _session = session;
        _httpClient = httpClient;
        _transferState = transferState;

        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
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

            // 🔹 0. Завантажуємо статистику відповідальності
            var valuationResponse = await _httpClient.GetAsync($"tool-valuations/worker/{user.Id}");
            if (valuationResponse.IsSuccessStatusCode)
            {
                var stats = await valuationResponse.Content.ReadFromJsonAsync<WorkerOnHandsStatsDto>(
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (stats != null)
                {
                    TotalLiabilityText = $"{stats.TotalValue:F2} UAH";
                    ToolCountText = $"{stats.ToolCount} tool{(stats.ToolCount == 1 ? "" : "s")}";
                }
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
                }
                catch
                {
                    // Якщо фото немає / впала помилка — просто пропускаємо
                }

                typeDict.TryGetValue(tool.ToolTypeId, out var typeTitle);
                typeTitle ??= "Невідомий тип";

                // Fetch the active assignment to get DueAt / IsOverdue
                ToolAssignmentDto? assignment = null;
                try
                {
                    var assignResp = await _httpClient.GetAsync(
                        $"tool-assignments/user/{user.Id}/tool/{tool.Id}/search-active");
                    if (assignResp.IsSuccessStatusCode)
                    {
                        assignment = await assignResp.Content.ReadFromJsonAsync<ToolAssignmentDto>(
                            new System.Text.Json.JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                    }
                }
                catch
                {
                    // Non-fatal — tool displays without deadline info
                }

                var capturedTool = tool;
                var capturedPhoto = photo;
                var capturedTypeTitle = typeTitle;

                Tools.Add(new ToolItemViewModel
                {
                    Id = capturedTool.Id,
                    SerialNumber = capturedTool.SerialNumber,
                    Photo = capturedPhoto,
                    ToolTypeTitle = capturedTypeTitle,
                    Price = capturedTool.Price,
                    DueAt = assignment?.DueAt,
                    IsOverdue = assignment?.IsOverdue ?? false,
                    TransferCommand = new Command(async () =>
                        await OnTransferToolAsync(
                            capturedTool.Id,
                            capturedTypeTitle,
                            capturedTool.SerialNumber,
                            capturedPhoto))
                });
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.ToString(), "OK");
        }
    }

    private async Task OnTransferToolAsync(
        Guid toolId,
        string typeTitle,
        string? serialNumber,
        ImageSource? photo)
    {
        _transferState.SelectedTool = new ToolItemViewModel
        {
            Id = toolId,
            ToolTypeTitle = typeTitle,
            SerialNumber = serialNumber,
            Photo = photo
        };

        await Shell.Current.GoToAsync(nameof(TransferPage));
    }
}
