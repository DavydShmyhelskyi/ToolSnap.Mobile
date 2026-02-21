using Microsoft.Maui.Media;
using System.Collections.Generic;
using System.Linq;
using ToolSnap.Mobile.Services;
using ToolSnap.Mobile.Dtos;

namespace ToolSnap.Mobile.Pages;

public partial class ReturnPage : ContentPage
{
    private readonly List<FileResult> _selectedPhotos = new();
    private readonly ToolTakeService _toolTakeService;          // той самий сервіс
    private readonly DetectionParsingService _parsingService;
    private readonly TakeFlowStateService _takeFlowState;

    public ReturnPage(
        ToolTakeService toolTakeService,
        DetectionParsingService parsingService,
        TakeFlowStateService takeFlowState)
    {
        InitializeComponent();
        _toolTakeService = toolTakeService;
        _parsingService = parsingService;
        _takeFlowState = takeFlowState;
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
                ReturnButton.IsEnabled = _selectedPhotos.Any();
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

                ReturnButton.IsEnabled = _selectedPhotos.Any();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }

    private async void OnReturnClicked(object? sender, EventArgs e)
    {
        if (!_selectedPhotos.Any())
        {
            await DisplayAlertAsync("No Photos", "Please take or choose photos first.", "OK");
            return;
        }

        try
        {
            var result = await _toolTakeService.ReturnToolsAsync(_selectedPhotos);

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

            var detections = _parsingService.ParseDetections(result.DetectionRawJson);

            if (detections.Count == 0)
            {
                await DisplayAlertAsync("Gemini", "No tools detected.", "OK");
                return;
            }

            // 🔹 Зберігаємо стан – той самий TakeFlowStateService
            _takeFlowState.CurrentSession = result.Session;
            _takeFlowState.CurrentDetections = detections.ToList();

            await Shell.Current.GoToAsync(nameof(ConfirmOnReturnToolAsignmentPage));

            _selectedPhotos.Clear();
            PreviewImage.Source = null;
            ReturnButton.IsEnabled = false;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }
}