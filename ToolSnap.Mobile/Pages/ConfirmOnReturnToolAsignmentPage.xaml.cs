using System.Collections.ObjectModel;
using ToolSnap.Mobile.Dtos;
using ToolSnap.Mobile.Models;
using ToolSnap.Mobile.Services;

namespace ToolSnap.Mobile.Pages;

public partial class ConfirmOnReturnToolAsignmentPage : ContentPage
{
    private readonly ToolConfirmationService _confirmationService;
    private readonly TakeFlowStateService _takeFlowState;
    private readonly UserSessionService _userSession;

    public PhotoSessionDto? PhotoSession { get; set; }
    public List<GeminiDetection>? Detections { get; set; }

    public ObservableCollection<ConfirmDetectedToolItem> Items { get; } = new();

    public ConfirmOnReturnToolAsignmentPage(
        ToolConfirmationService confirmationService,
        TakeFlowStateService takeFlowState,
        UserSessionService userSession)
    {
        InitializeComponent();
        _confirmationService = confirmationService;
        _takeFlowState = takeFlowState;
        _userSession = userSession;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (Items.Count > 0)
            return;

        PhotoSession = _takeFlowState.CurrentSession;
        Detections = _takeFlowState.CurrentDetections;

        if (PhotoSession is null || Detections is null || Detections.Count == 0)
        {
            await DisplayAlertAsync("Error",
                "No session or detections available for confirmation.",
                "OK");
            return;
        }

        try
        {
            var (toolTypes, brands, models) = await _confirmationService.LoadDictionariesAsync();

            Items.Clear();

            foreach (var det in Detections)
            {
                var item = new ConfirmDetectedToolItem(
                    photoSessionId: PhotoSession.Id,
                    confidence: det.Confidence,
                    redFlagged: det.RedFlagged,
                    toolTypes: toolTypes,
                    brands: brands);

                item.SetModels(models);

                item.SelectedToolType = toolTypes
                    .FirstOrDefault(t =>
                        string.Equals(t.Title, det.ToolType, StringComparison.OrdinalIgnoreCase));

                item.SelectedBrand = !string.IsNullOrWhiteSpace(det.Brand)
                    ? brands.FirstOrDefault(b =>
                        string.Equals(b.Title, det.Brand, StringComparison.OrdinalIgnoreCase))
                    : null;

                item.SelectedModel = !string.IsNullOrWhiteSpace(det.Model)
                    ? models.FirstOrDefault(m =>
                        string.Equals(m.Title, det.Model, StringComparison.OrdinalIgnoreCase))
                    : null;

                item.SerialNumber = null;

                // ❗ ВАЖЛИВО: для return-флоу тут викликаємо пошук не returned tools by user
                await RefreshToolsForItemAsync(item);

                item.PropertyChanged += async (_, e) =>
                {
                    if (e.PropertyName == nameof(ConfirmDetectedToolItem.SelectedToolType) ||
                        e.PropertyName == nameof(ConfirmDetectedToolItem.SelectedBrand) ||
                        e.PropertyName == nameof(ConfirmDetectedToolItem.SelectedModel))
                    {
                        await RefreshToolsForItemAsync(item);
                    }
                };

                Items.Add(item);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }

    private async Task RefreshToolsForItemAsync(ConfirmDetectedToolItem item)
    {
        try
        {
            if (item.SelectedToolType is null)
            {
                item.SetTools(new List<ToolDto>());
                return;
            }

            if (!_userSession.IsLoggedIn || _userSession.CurrentUser is null)
            {
                await DisplayAlertAsync("Error", "User is not logged in.", "OK");
                item.SetTools(new List<ToolDto>());
                return;
            }

            var userId = _userSession.CurrentUser.Id;

            var tools = await _confirmationService.SearchNotReturnedToolsByUserAsync(
                userId,
                item.SelectedToolType.Id,
                item.SelectedBrand?.Id,
                item.SelectedModel?.Id,
                CancellationToken.None);

            item.SetTools(tools);

            // якщо знайдено рівно один тул – логічно відразу вибрати його
            if (tools.Count == 1)
            {
                item.SelectedTool = tools[0];
            }

            // DEBUG – щоб ти бачив, що тепер дійсно щось приходить
            await DisplayAlertAsync(
                "DEBUG TOOLS (RETURN)",
                $"User: {userId}\n" +
                $"Type: {item.SelectedToolType?.Title}\n" +
                $"Brand: {item.SelectedBrand?.Title ?? "-"}\n" +
                $"Model: {item.SelectedModel?.Title ?? "-"}\n\n" +
                $"Loaded not-returned tools: {tools.Count}",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Tools load error", ex.Message, "OK");
        }
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        if (PhotoSession is null)
            return;

        if (!_userSession.IsLoggedIn || _userSession.CurrentUser is null)
        {
            await DisplayAlertAsync("Error", "User is not logged in.", "OK");
            return;
        }

        var userId = _userSession.CurrentUser.Id;

        try
        {
            var result = await _confirmationService.ConfirmReturnAsync(
                userId,
                PhotoSession,
                Items.ToList(),
                CancellationToken.None);

            if (!result.Success)
            {
                await DisplayAlertAsync("Error",
                    result.ErrorMessage ?? "Failed to confirm return.",
                    "OK");
                return;
            }

            await DisplayAlertAsync("Success",
                "Tools returned successfully.",
                "OK");

            _takeFlowState.Clear();
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }
}