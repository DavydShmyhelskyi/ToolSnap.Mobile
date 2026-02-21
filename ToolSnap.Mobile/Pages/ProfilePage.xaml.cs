using System.Net.Http;
using System.Net.Http.Json;
using ToolSnap.Mobile.Dtos;
using ToolSnap.Mobile.Services;

namespace ToolSnap.Mobile.Pages;

public partial class ProfilePage : ContentPage
{
    private readonly HttpClient _httpClient;
    private readonly UserSessionService _session;

    public ProfilePage(HttpClient httpClient, UserSessionService session)
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
            // Якщо раптом юзера немає в сесії – відправляємо на логін
            Shell.Current.GoToAsync("//login");
            return;
        }

        // Заповнюємо інформацію про юзера (read-only)
        FullNameLabel.Text = user.FullName;
        EmailLabel.Text = user.Email;
        ConfirmedEmailLabel.Text = user.ConfirmedEmail ? "Yes" : "No";
        IsActiveLabel.Text = user.IsActive ? "Active" : "Inactive";
        CreatedAtLabel.Text = user.CreatedAt.ToString("g");
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

        // Простенька валідація на клієнті
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
                currentPassword = currentPassword,
                newPassword = newPassword
            };

            var request = new HttpRequestMessage(
                HttpMethod.Patch,
                $"users/{user.Id}/change-password")
            {
                Content = JsonContent.Create(body)
            };

            var response = await _httpClient.SendAsync(request);

            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlertAsync(
                    "Change password failed",
                    $"{response.StatusCode}\n{responseText}",
                    "OK");
                return;
            }

            // Сервер повертає оновлений UserDto (без пароля)
            var updatedUser = await response.Content.ReadFromJsonAsync<UserDto>();

            if (updatedUser is not null)
            {
                // Можемо оновити сесію (на всяк випадок)
                _session.SetUser(updatedUser);
            }

            await DisplayAlertAsync("Success", "Password updated successfully.", "OK");

            // очищаємо поля
            CurrentPasswordEntry.Text = string.Empty;
            NewPasswordEntry.Text = string.Empty;
            ConfirmPasswordEntry.Text = string.Empty;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }
}