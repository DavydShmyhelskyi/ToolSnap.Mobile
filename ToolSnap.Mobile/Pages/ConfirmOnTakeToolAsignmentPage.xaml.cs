using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using ToolSnap.Mobile.Dtos;
using ToolSnap.Mobile.Models;
using ToolSnap.Mobile.Services;

namespace ToolSnap.Mobile.Pages;

public partial class ConfirmOnTakeToolAsignmentPage : ContentPage
{
    private readonly ToolConfirmationService _confirmationService;
    private readonly TakeFlowStateService _takeFlowState;
    private readonly UserSessionService _userSession;

    public PhotoSessionDto? PhotoSession { get; set; }
    public List<GeminiDetection>? Detections { get; set; }

    // 🔹 ObservableCollection, щоб CollectionView автоматично оновлювався
    public ObservableCollection<ConfirmDetectedToolItem> Items { get; } = new();

    public ConfirmOnTakeToolAsignmentPage(
        ToolConfirmationService confirmationService,
        TakeFlowStateService takeFlowState,
        UserSessionService userSession)
    {
        InitializeComponent();
        _confirmationService = confirmationService;
        _takeFlowState = takeFlowState;
        BindingContext = this;
        _userSession = userSession;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // щоб не ініціалізувати вдруге
        if (Items.Count > 0)
            return;

        // Беремо стан з TakeFlowStateService
        PhotoSession = _takeFlowState.CurrentSession;
        Detections = _takeFlowState.CurrentDetections;

        if (PhotoSession is null || Detections is null || Detections.Count == 0)
        {
            await DisplayAlertAsync(
                "Error",
                "No session or detections available for confirmation.",
                "OK");
            return;
        }

        try
        {
            // 1️⃣ Тягнемо довідники
            var (toolTypes, brands, models) = await _confirmationService.LoadDictionariesAsync();

            // 2️⃣ На всякий випадок чистимо колекцію
            Items.Clear();

            // 3️⃣ Будуємо Items
            foreach (var det in Detections)
            {
                var item = new ConfirmDetectedToolItem(
                    photoSessionId: PhotoSession.Id,
                    confidence: det.Confidence,
                    redFlagged: det.RedFlagged,
                    toolTypes: toolTypes,
                    brands: brands);

                // Спільний список моделей
                item.SetModels(models);

                // Тип за назвою
                item.SelectedToolType = toolTypes
                    .FirstOrDefault(t =>
                        string.Equals(t.Title, det.ToolType, StringComparison.OrdinalIgnoreCase));

                // Бренд за назвою
                item.SelectedBrand = !string.IsNullOrWhiteSpace(det.Brand)
                    ? brands.FirstOrDefault(b =>
                        string.Equals(b.Title, det.Brand, StringComparison.OrdinalIgnoreCase))
                    : null;

                // Модель за назвою (якщо є)
                item.SelectedModel = !string.IsNullOrWhiteSpace(det.Model)
                    ? models.FirstOrDefault(m =>
                        string.Equals(m.Title, det.Model, StringComparison.OrdinalIgnoreCase))
                    : null;

                item.SerialNumber = null;

                // Перше завантаження інструментів
                _ = RefreshToolsForItemAsync(item);

                // При зміні типу/бренду/моделі – переліваємо Tool-си
                item.PropertyChanged += async (_, e) =>
                {
                    if (e.PropertyName == nameof(ConfirmDetectedToolItem.SelectedToolType) ||
                        e.PropertyName == nameof(ConfirmDetectedToolItem.SelectedBrand) ||
                        e.PropertyName == nameof(ConfirmDetectedToolItem.SelectedModel))
                    {
                        await RefreshToolsForItemAsync(item);
                    }
                };

                // 🔹 Додаємо в ObservableCollection – CollectionView сам оновиться
                Items.Add(item);
            }

            // ⛔ НІЧОГО не треба робити з ItemsCollectionView.ItemsSource в коді,
            // бо в XAML уже є: ItemsSource="{Binding Items}"
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }

    private async System.Threading.Tasks.Task RefreshToolsForItemAsync(ConfirmDetectedToolItem item)
    {
        try
        {
            if (item.SelectedToolType is null)
            {
                item.SetTools(new List<ToolDto>());
                return;
            }

            var tools = await _confirmationService.SearchToolsAsync(
                toolTypeId: item.SelectedToolType.Id,
                brandId: item.SelectedBrand?.Id,
                modelId: item.SelectedModel?.Id,
                cancellationToken: CancellationToken.None);

            item.SetTools(tools);

            if (!string.IsNullOrEmpty(item.SerialNumber))
            {
                var match = tools.FirstOrDefault(t =>
                    string.Equals(t.SerialNumber, item.SerialNumber, StringComparison.OrdinalIgnoreCase));
                if (match is not null)
                    item.SelectedTool = match;
            }
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
        try
        {
            // TODO: взяти реальний userId з UserSessionService
            var userId = _userSession.CurrentUser.Id;

            var result = await _confirmationService.ConfirmAsync(
                userId,
                PhotoSession,
                Items.ToList(),            // ObservableCollection теж IList, але тут робимо List для зручності
                CancellationToken.None);

            if (!result.Success)
            {
                await DisplayAlertAsync("Error",
                    result.ErrorMessage ?? "Failed to confirm tools.",
                    "OK");
                return;
            }

            await DisplayAlertAsync("Success",
                "Tools assigned successfully.",
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