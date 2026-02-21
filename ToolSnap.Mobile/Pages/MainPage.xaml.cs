using System.Net.Http.Json;
using System.Text.Json;
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
            var loginRequest = new LoginDto(
                EmailEntry.Text.Trim(),
                PasswordEntry.Text
            );

            var response = await _httpClient.PostAsJsonAsync("auth/login", loginRequest);
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

            var authResponse = JsonSerializer.Deserialize<AuthenticationResponseDto>(
                responseText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (authResponse == null)
            {
                await DisplayAlertAsync("Error", "Invalid authentication data received", "OK");
                return;
            }

            await _session.SetUserAsync(authResponse);

            await DisplayAlertAsync("Success", $"Welcome {authResponse.FullName}", "OK");

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