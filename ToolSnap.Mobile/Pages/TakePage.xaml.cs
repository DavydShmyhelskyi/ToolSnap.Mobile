using Microsoft.Maui.Media;
using System.Collections.Generic;
using System.Linq;
using ToolSnap.Mobile.Services;

namespace ToolSnap.Mobile.Pages;

public partial class TakePage : ContentPage
{
    private readonly List<FileResult> _selectedPhotos = new();
    private readonly ToolTakeService _toolTakeService;
    private readonly DetectionParsingService _parsingService;
    private readonly DetectedToolsService _detectedToolsService;
    private readonly TakeFlowStateService _takeFlowState;
    public TakePage(ToolTakeService toolTakeService, DetectionParsingService parsingService, DetectedToolsService detectedToolsService, TakeFlowStateService takeFlowState)
    {
        InitializeComponent();
        _toolTakeService = toolTakeService;
        _parsingService = parsingService;
        _detectedToolsService = detectedToolsService;
        _takeFlowState = takeFlowState;
    }

    public static readonly BindableProperty IsLoadingProperty =
        BindableProperty.Create(
            nameof(IsLoading),
            typeof(bool),
            typeof(TakePage),
            false);

    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    private async void OnCameraClicked(object? sender, EventArgs e)
    {
        try
        {
            var photo = await MediaPicker.CapturePhotoAsync();

            if (photo != null)
            {
                _selectedPhotos.Add(photo);

                var stream = await photo.OpenReadAsync();
                PreviewImage.Source = ImageSource.FromStream(() => stream);
                TakeButton.IsEnabled = _selectedPhotos.Any();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }

    private async void OnGalleryClicked(object? sender, EventArgs e)
    {
        try
        {
            var photos = await MediaPicker.PickPhotosAsync();

            if (photos != null && photos.Any())
            {
                _selectedPhotos.AddRange(photos);

                var last = _selectedPhotos.Last();
                var stream = await last.OpenReadAsync();
                PreviewImage.Source = ImageSource.FromStream(() => stream);

                TakeButton.IsEnabled = _selectedPhotos.Any();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }

    private async void OnTakeClicked(object? sender, EventArgs e)
    {
        if (!_selectedPhotos.Any())
        {
            await DisplayAlertAsync("No Photos",
                "Please take or choose photos first.",
                "OK");
            return;
        }

        try
        {
            IsLoading = true;
            TakeButton.IsEnabled = false;

            await DisplayAlertAsync("Step 1",
                $"Sending {_selectedPhotos.Count} photo(s) to API...",
                "OK");

            var result = await _toolTakeService.TakeToolsAsync(_selectedPhotos);

            if (!result.Success)
            {
                await DisplayAlertAsync("Error",
                    result.ErrorMessage ?? "Unknown error.",
                    "OK");
                return;
            }

            if (result.Session is null)
            {
                await DisplayAlertAsync("Error",
                    "No PhotoSession returned from API.",
                    "OK");
                return;
            }

            await DisplayAlertAsync("Step 2",
                $"PhotoSession created:\nId: {result.Session.Id}\n" +
                $"Lat: {result.Session.Latitude}, Lng: {result.Session.Longitude}",
                "OK");

            // 🔹 показуємо raw-відповідь від бекенду (де всередині є JSON від Gemini)
            var raw = result.DetectionRawJson ?? "<null>";
            var shortRaw = raw.Length > 400 ? raw[..400] + "..." : raw;

            await DisplayAlertAsync("Gemini Raw",
                shortRaw,
                "OK");

            var detections = _parsingService.ParseDetections(result.DetectionRawJson);

            await DisplayAlertAsync("Step 3",
                $"Parsed detections: {detections.Count}",
                "OK");

            if (detections.Count == 0)
            {
                await DisplayAlertAsync("Gemini",
                    "No tools detected.",
                    "OK");
                return;
            }

            // невеликий summary по кожному detection
            var summary = string.Join("\n\n", detections.Select((d, i) =>
                $"#{i + 1}\n" +
                $"Type: {d.ToolType}\n" +
                $"Brand: {d.Brand ?? "-"}\n" +
                $"Model: {d.Model ?? "-"}\n" +
                $"Confidence: {d.Confidence}\n" +
                $"RedFlagged: {d.RedFlagged}"));

            await DisplayAlertAsync("Gemini Parsed", summary, "OK");

            // 🔹 ЗБЕРІГАЄМО СТАН ДЛЯ НАСТУПНОЇ СТОРІНКИ
            _takeFlowState.CurrentSession = result.Session;
            _takeFlowState.CurrentDetections = detections.ToList();

            await DisplayAlertAsync("Step 4",
                "Navigating to Confirm page...",
                "OK");

            // 🔹 ПЕРЕХІД БЕЗ ПАРАМЕТРІВ (стан уже в сервісі)
            await Shell.Current.GoToAsync(nameof(ConfirmOnTakeToolAsignmentPage));

            _selectedPhotos.Clear();
            PreviewImage.Source = null;
            TakeButton.IsEnabled = false;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

}
