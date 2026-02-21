using Microsoft.Maui.Storage;
using System.Net.Http.Json;
using System.Text.Json;
using ToolSnap.Mobile.Dtos;

namespace ToolSnap.Mobile.Services;

public sealed class UserSessionService
{
    private const string UserKey = "current_user";
    private readonly AuthTokenService _tokenService;

    public UserSessionService(AuthTokenService tokenService)
    {
        _tokenService = tokenService;
    }

    public UserDto? CurrentUser { get; private set; }

    public bool IsLoggedIn => CurrentUser != null;

    public async Task SetUserAsync(AuthenticationResponseDto authResponse)
    {
        CurrentUser = new UserDto(
            authResponse.Id,
            authResponse.FullName,
            authResponse.Email,
            authResponse.Role,
            authResponse.IsActive,
            authResponse.EmailConfirmed);

        var json = JsonSerializer.Serialize(CurrentUser);
        Preferences.Set(UserKey, json);

        await _tokenService.SetTokensAsync(authResponse.AccessToken, authResponse.RefreshToken);
    }

    public void LoadUser()
    {
        var json = Preferences.Get(UserKey, null);

        if (!string.IsNullOrEmpty(json))
        {
            CurrentUser = JsonSerializer.Deserialize<UserDto>(json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
    }

    public async Task LogoutAsync(HttpClient httpClient)
    {
        try
        {
            var refreshToken = await _tokenService.GetRefreshTokenAsync();

            if (!string.IsNullOrEmpty(refreshToken))
            {
                await httpClient.PostAsJsonAsync("auth/revoke", new RefreshTokenDto(refreshToken));
            }
        }
        catch
        {
            // Silent fail - still clear local data
        }
        finally
        {
            CurrentUser = null;
            Preferences.Remove(UserKey);
            _tokenService.ClearTokens();
        }
    }
}
