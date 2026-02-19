using System.Net.Http.Json;
using System.Text.Json;
using ToolSnap.Mobile.Dtos;

namespace ToolSnap.Mobile;

public partial class MainPage : ContentPage
{
    private readonly HttpClient _httpClient;

    public MainPage()
    {
        InitializeComponent();

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5029/") 
            // якщо Windows: http://localhost:5029/ http://10.0.2.2:5029/
        };
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        try
        {
            var loginRequest = new
            {
                email = EmailEntry.Text,
                password = PasswordEntry.Text
            };

            var response = await _httpClient.PostAsJsonAsync("users/login", loginRequest);

            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlertAsync("Login Failed",
                    $"{response.StatusCode}\n{responseText}",
                    "OK");
                return;
            }

            // десеріалізація UserDto
            var user = JsonSerializer.Deserialize<UserDto>(responseText,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            await DisplayAlertAsync("User DTO",
                $"Id: {user?.Id}\n" +
                $"Name: {user?.FullName}\n" +
                $"Email: {user?.Email}\n" +
                $"Active: {user?.IsActive}",
                "OK");
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


