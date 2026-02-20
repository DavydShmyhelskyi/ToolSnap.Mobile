using Microsoft.Maui.Media;
using System.Collections.Generic;
using System.Linq;
using ToolSnap.Mobile.Services;

namespace ToolSnap.Mobile.Pages;

public partial class TakePage : ContentPage
{
    private readonly List<FileResult> _selectedPhotos = new();
    private readonly ToolTakeService _toolTakeService;
    public TakePage(ToolTakeService toolTakeService)
    {
        InitializeComponent();
        _toolTakeService = toolTakeService;
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

    private async void OnCameraClicked(object sender, EventArgs e)
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

    private async void OnGalleryClicked(object sender, EventArgs e)
    {
        try
        {
            var photos = await MediaPicker.PickPhotosAsync();

            if (photos != null && photos.Any())
            {
                // додамо всі вибрані фото до списку
                _selectedPhotos.AddRange(photos);

                // для превʼю покажемо останнє
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

    private async void OnTakeClicked(object sender, EventArgs e)
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

            var result = await _toolTakeService.TakeToolsAsync(_selectedPhotos);

            if (!result.Success)
            {
                await DisplayAlertAsync("Error",
                    result.ErrorMessage ?? "Unknown error.",
                    "OK");
                return;
            }

            // Показуємо відповідь Gemini
            await DisplayAlertAsync(
                "Gemini Response",
                result.DetectionRawJson ?? "No detection data",
                "OK");

            // Якщо хочеш, можеш також показати SessionId:
            await DisplayAlertAsync(
                "Session Created",
                $"Session ID: {result.Session?.Id}",
                "OK");

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
            TakeButton.IsEnabled = _selectedPhotos.Any();
        }
    }
}
