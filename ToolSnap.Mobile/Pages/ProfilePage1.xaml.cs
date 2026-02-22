using System.Net.Http;
using System.Net.Http.Json;
using ToolSnap.Mobile.Dtos;
using ToolSnap.Mobile.Services;

namespace ToolSnap.Mobile.Pages;

public partial class ProfilePage1 : ContentPage
{
    private readonly HttpClient _httpClient;
    private readonly UserSessionService _session;

    public ProfilePage1(HttpClient httpClient, UserSessionService session)
    {
        InitializeComponent();

        _httpClient = httpClient;
        _session = session;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        var user = _session.CurrentUser;

        if (user is null)
        {
            Shell.Current.GoToAsync("//login");
            return;
        }

        FullNameLabel.Text = user.FullName;
        EmailLabel.Text = user.Email;
        ConfirmedEmailLabel.Text = user.ConfirmedEmail ? "Yes" : "No";
        IsActiveLabel.Text = user.IsActive ? "Active" : "Inactive";
        CreatedAtLabel.Text = user.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");

        if (user.Latitude is double lat && user.Longitude is double lon)
            CoordinatesLabel.Text = $"{lat:F5}, {lon:F5}";
        else
            CoordinatesLabel.Text = "Not set";
    }

    private void OnTogglePasswordPanelClicked(object sender, EventArgs e)
    {
        PasswordPanel.IsVisible = !PasswordPanel.IsVisible;

        TogglePasswordPanelButton.Text =
            PasswordPanel.IsVisible ? "Hide password form" : "Change password";
    }

    private async void OnChangePasswordClicked(object sender, EventArgs e)
    {
        var user = _session.CurrentUser;

        if (user is null)
        {
            await DisplayAlertAsync("Error", "User not found. Please login again.", "OK");
            await Shell.Current.GoToAsync("//login");
            return;
        }

        var currentPassword = CurrentPasswordEntry.Text;
        var newPassword = NewPasswordEntry.Text;
        var confirmPassword = ConfirmPasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(currentPassword) ||
            string.IsNullOrWhiteSpace(newPassword) ||
            string.IsNullOrWhiteSpace(confirmPassword))
        {
            await DisplayAlertAsync("Error", "All fields are required.", "OK");
            return;
        }

        if (newPassword != confirmPassword)
        {
            await DisplayAlertAsync("Error", "New passwords do not match.", "OK");
            return;
        }

        if (newPassword.Length < 6)
        {
            await DisplayAlertAsync("Error", "New password must be at least 6 characters.", "OK");
            return;
        }

        try
        {
            var body = new
            {
                currentPassword,
                newPassword
            };

            var request = new HttpRequestMessage(
                HttpMethod.Patch,
                $"users/{user.Id}/change-password")
            {
                Content = JsonContent.Create(body)
            };

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                await DisplayAlertAsync("Failed", errorText, "OK");
                return;
            }

            var updatedUser = await response.Content.ReadFromJsonAsync<UserDto>();
            if (updatedUser is not null)
                _session.SetUser(updatedUser);

            await DisplayAlertAsync("Success", "Password updated successfully.", "OK");

            CurrentPasswordEntry.Text = "";
            NewPasswordEntry.Text = "";
            ConfirmPasswordEntry.Text = "";
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }
}