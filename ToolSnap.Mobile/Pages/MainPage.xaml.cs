using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Maui.Devices.Sensors;
using ToolSnap.Mobile.Dtos;
using ToolSnap.Mobile.Services;

namespace ToolSnap.Mobile;

public partial class MainPage : ContentPage
{
    private readonly HttpClient _httpClient;
    private readonly UserSessionService _session;

    public MainPage(HttpClient httpClient, UserSessionService session)
    {
        InitializeComponent();

        _httpClient = httpClient;
        _session = session;

        _session.LoadUser();

        if (_session.IsLoggedIn)
        {
            DisplayAlertAsync(
                "Already logged in",
                $"Welcome back {_session.CurrentUser?.FullName}",
                "OK");
        }
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        try
        {
            // 🔥 Геолокація (необовʼязково, але контролер її очікує)
            double longitude = 0;
            double latitude = 0;

            try
            {
                var loc = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium));

                if (loc != null)
                {
                    longitude = loc.Longitude;
                    latitude = loc.Latitude;
                }
            }
            catch
            {
                // Глушимо — сервер прийме 0,0
            }

            // 🔥 DTO повністю відповідає контролеру
            var loginRequest = new LoginDto(
                EmailEntry.Text.Trim(),
                PasswordEntry.Text,
                longitude,
                latitude
            );

            var response = await _httpClient.PostAsJsonAsync("users/login", loginRequest);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlertAsync(
                    "Login Failed",
                    $"{response.StatusCode}\n{responseText}",
                    "OK"
                );
                return;
            }

            // 🔥 Твій UserDto — ідеально підходить під відповідь сервера
            var user = JsonSerializer.Deserialize<UserDto>(
                responseText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (user == null)
            {
                await DisplayAlertAsync("Error", "Invalid user data received", "OK");
                return;
            }

            // 🔥 Зберігаємо користувача
            _session.SetUser(user);

            await DisplayAlertAsync("Success", $"Welcome {user.FullName}", "OK");

            await Shell.Current.GoToAsync("//home");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }

    private async void OnButtonPressed(object sender, EventArgs e)
    {
        await LoginButton.ScaleToAsync(0.9, 100);
    }

    private async void OnButtonReleased(object sender, EventArgs e)
    {
        await LoginButton.ScaleToAsync(1, 100);
    }
}