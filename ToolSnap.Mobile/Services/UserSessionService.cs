using Microsoft.Maui.Storage;
using System.Text.Json;
using ToolSnap.Mobile.Dtos;

namespace ToolSnap.Mobile.Services;

public sealed class UserSessionService
{
    private const string UserKey = "current_user";

    public UserDto? CurrentUser { get; private set; }

    public bool IsLoggedIn => CurrentUser != null;

    public void SetUser(UserDto user)
    {
        CurrentUser = user;

        var json = JsonSerializer.Serialize(user);
        Preferences.Set(UserKey, json);
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

    public void Logout()
    {
        CurrentUser = null;
        Preferences.Remove(UserKey);
    }
}
