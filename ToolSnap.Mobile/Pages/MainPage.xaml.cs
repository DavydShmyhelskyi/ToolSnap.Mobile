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
    private readonly FcmTokenService _fcmTokenService;
    private readonly BiometricService _bioService;

    public MainPage(
        HttpClient httpClient,
        UserSessionService session,
        FcmTokenService fcmTokenService,
        BiometricService bioService)
    {
        InitializeComponent();

        _httpClient = httpClient;
        _session = session;
        _fcmTokenService = fcmTokenService;
        _bioService = bioService;

        _session.LoadUser();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_bioService.IsEnabled)
        {
            if (await _bioService.IsAvailableAsync())
            {
                BiometricSection.IsVisible = true;
                await TryBiometricLoginAsync();
            }
            else
            {
                _bioService.Disable();
                BiometricSection.IsVisible = false;
            }
        }
        else
        {
            BiometricSection.IsVisible = false;
        }
    }

    private async void OnBiometricClicked(object sender, EventArgs e)
    {
        await TryBiometricLoginAsync();
    }

    private async Task TryBiometricLoginAsync()
    {
        var authenticated = await _bioService.AuthenticateAsync();
        if (!authenticated)
            return;

        var (email, password) = await _bioService.GetCredentialsAsync();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            _bioService.Disable();
            BiometricSection.IsVisible = false;
            await DisplayAlertAsync("Biometrics", "Stored credentials not found. Please sign in with your password.", "OK");
            return;
        }

        await PerformLoginAsync(email, password, 0, 0, isBiometric: true);
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim();
        var password = PasswordEntry.Text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            await DisplayAlertAsync("Validation", "Email and password are required.", "OK");
            return;
        }

        double longitude = 0;
        double latitude = 0;

        try
        {
            var loc = await Geolocation.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Medium));

            if (loc != null)
            {
                longitude = loc.Longitude;
                latitude = loc.Latitude;

                await DisplayAlertAsync(
                    "GPS координації",
                    $"Latitude: {latitude}\nLongitude: {longitude}",
                    "OK");
            }
            else
            {
                await DisplayAlertAsync("GPS", "Не вдалося отримати дані геолокації", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("GPS Error", $"Не вдалося отримати координати.\n{ex.Message}", "OK");
        }

        await PerformLoginAsync(email, password, longitude, latitude, isBiometric: false);
    }

    private async Task PerformLoginAsync(string email, string password, double longitude, double latitude, bool isBiometric)
    {
        try
        {
            var loginRequest = new LoginDto(email, password, longitude, latitude);
            var response = await _httpClient.PostAsJsonAsync("users/login", loginRequest);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                if (isBiometric)
                {
                    _bioService.Disable();
                    BiometricSection.IsVisible = false;
                    await DisplayAlertAsync("Login Failed", "Biometric sign-in failed. Please sign in with your password.", "OK");
                }
                else
                {
                    await DisplayAlertAsync("Login Failed", $"{response.StatusCode}\n{responseText}", "OK");
                }
                return;
            }

            var user = JsonSerializer.Deserialize<UserDto>(
                responseText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (user == null)
            {
                await DisplayAlertAsync("Error", "Invalid user data received", "OK");
                return;
            }

            _session.SetUser(user);
            _ = _fcmTokenService.RegisterAsync(user.Id);

            if (!isBiometric && !_bioService.IsEnabled && await _bioService.IsAvailableAsync())
            {
                var enable = await DisplayAlertAsync(
                    "Enable Biometrics",
                    "Would you like to sign in with fingerprint or Face ID next time?",
                    "Enable",
                    "Not now");

                if (enable)
                    await _bioService.EnableAsync(email, password);
            }

            if (!isBiometric)
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
