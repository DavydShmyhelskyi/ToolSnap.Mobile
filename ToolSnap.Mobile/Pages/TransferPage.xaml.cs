using System.Text.Json;
using ToolSnap.Mobile.Dtos;
using ToolSnap.Mobile.Services;

namespace ToolSnap.Mobile.Pages;

public partial class TransferPage : ContentPage
{
    private readonly ToolTransferService _transferService;
    private readonly TransferFlowStateService _state;
    private readonly UserSessionService _session;
    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    public List<UserDto> Users { get; private set; } = new();

    private UserDto? _selectedRecipient;

    public TransferPage(
        ToolTransferService transferService,
        TransferFlowStateService state,
        UserSessionService session,
        HttpClient httpClient)
    {
        InitializeComponent();
        _transferService = transferService;
        _state = state;
        _session = session;
        _httpClient = httpClient;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var tool = _state.SelectedTool;
        if (tool == null)
        {
            await DisplayAlert("Error", "No tool selected.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        ToolTypeLabel.Text = tool.ToolTypeTitle;
        ToolSerialLabel.Text = tool.SerialNumber ?? "—";

        if (tool.Photo != null)
            ToolPhoto.Source = tool.Photo;

        _selectedRecipient = null;
        RecipientPicker.SelectedIndex = -1;
        ConfirmButton.IsEnabled = false;

        await LoadUsersAsync();
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("users");
            var text = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlert("Error", $"Failed to load users: {text}", "OK");
                return;
            }

            var allUsers = JsonSerializer.Deserialize<List<UserDto>>(text, JsonOptions)
                           ?? new List<UserDto>();

            var currentId = _session.CurrentUser?.Id;

            Users = allUsers
                .Where(u => u.Id != currentId && u.IsActive)
                .OrderBy(u => u.FullName)
                .ToList();

            OnPropertyChanged(nameof(Users));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private void OnRecipientSelected(object sender, EventArgs e)
    {
        _selectedRecipient = RecipientPicker.SelectedItem as UserDto;
        ConfirmButton.IsEnabled = _selectedRecipient != null;
    }

    private async void OnCameraClicked(object sender, EventArgs e)
    {
        try
        {
            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo != null)
                await ShowPreviewAsync(photo);
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
            var photos = await MediaPicker.Default.PickPhotosAsync();
            var photo = photos?.FirstOrDefault();
            if (photo != null)
                await ShowPreviewAsync(photo);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }

    private async Task ShowPreviewAsync(FileResult photo)
    {
        await using var stream = await photo.OpenReadAsync();
        var bytes = new byte[stream.Length];
        _ = await stream.ReadAsync(bytes);
        PhotoPreview.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
        PhotoPreviewBorder.IsVisible = true;
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        var tool = _state.SelectedTool;
        var recipient = _selectedRecipient;
        var currentUser = _session.CurrentUser;

        if (tool == null || recipient == null || currentUser == null)
            return;

        ConfirmButton.IsEnabled = false;

        var result = await _transferService.InitiateAsync(currentUser.Id, recipient.Id, tool.Id);

        if (!result.Success)
        {
            ConfirmButton.IsEnabled = true;
            await DisplayAlert("Error", result.ErrorMessage ?? "Transfer failed.", "OK");
            return;
        }

        _state.Clear();
        await AppShell.RefreshBadgeAsync(_transferService, _session);
        await Shell.Current.GoToAsync("..");
        await AppToast.ShowSuccessAsync($"Transfer request sent to {recipient.FullName}.");
    }
}
