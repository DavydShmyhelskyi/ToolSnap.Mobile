## JWT Authentication Integration - Complete ✅

### Що було зроблено:

#### 1. **Оновлено DTOs** (`UserDto.cs`)
- Додано `AuthenticationResponseDto` з `AccessToken` + `RefreshToken`
- `LoginDto` тепер без геолокації (відповідає бекенду)
- Додано `RefreshTokenDto` для refresh операцій
- `UserDto` тепер містить `Role` замість `RoleId`

#### 2. **Створено AuthTokenService** (`Services\AuthTokenService.cs`)
- Використовує `SecureStorage` для безпечного зберігання токенів
- Методи: `GetAccessTokenAsync`, `GetRefreshTokenAsync`, `SetTokensAsync`, `ClearTokens`

#### 3. **Створено AuthenticatedHttpClientHandler** (`Services\AuthenticatedHttpClientHandler.cs`)
- `DelegatingHandler` що автоматично додає `Authorization: Bearer {token}` до всіх запитів
- **Автоматичний refresh token** при 401 Unauthorized
- Виключає auth ендпоінти (`/auth/login`, `/auth/register`, `/auth/refresh`)
- При помилці refresh - очищає токени

#### 4. **Оновлено UserSessionService** (`Services\UserSessionService.cs`)
- `SetUserAsync` - приймає `AuthenticationResponseDto` і зберігає токени
- `LogoutAsync` - викликає бекенд `/auth/revoke` і очищає локальні дані
- Інтегрується з `AuthTokenService`

#### 5. **Оновлено MainPage.xaml.cs**
- Використовує `/auth/login` замість `/users/login`
- Зберігає токени через `SetUserAsync`
- Прибрано геолокацію з login

#### 6. **Оновлено MauiProgram.cs**
- Реєстрація `AuthTokenService` як Singleton
- `HttpClient` тепер використовує `AuthenticatedHttpClientHandler`
- Автоматична інтеграція з DI

---

### Як використовувати Logout:

#### У будь-якій сторінці (наприклад ProfilePage):

```csharp
private async void OnLogoutClicked(object sender, EventArgs e)
{
    var confirm = await DisplayAlert(
        "Logout", 
        "Are you sure you want to logout?", 
        "Yes", "No");
    
    if (!confirm) return;

    try
    {
        await _session.LogoutAsync(_httpClient);
        await Shell.Current.GoToAsync("//login");
    }
    catch (Exception ex)
    {
        await DisplayAlert("Error", ex.Message, "OK");
    }
}
```

#### У XAML додайте кнопку:

```xaml
<Button Text="Logout"
        Clicked="OnLogoutClicked"
        BackgroundColor="Red"
        TextColor="White" />
```

---

### Як працює автоматичний refresh:

1. Коли HttpClient отримує **401 Unauthorized**
2. `AuthenticatedHttpClientHandler` автоматично:
   - Бере `RefreshToken` з `SecureStorage`
   - Викликає `/auth/refresh`
   - Зберігає нові токени
   - **Автоматично повторює original request**
3. Якщо refresh fail - очищає токени (user must login)

---

### Що потрібно з бекенду:

**Ви вже все маєте!** ✅

Ваш бекенд повертає:
```csharp
public record AuthenticationResponseDto(
    Guid Id,
    string FullName,
    string Email,
    string Role,        // ← perfect
    bool IsActive,
    bool EmailConfirmed,
    string AccessToken,  // ← perfect
    string RefreshToken  // ← perfect
);
```

**Endpoints які використовуються:**
- `POST /auth/login` - логін
- `POST /auth/refresh` - автоматичний refresh
- `POST /auth/revoke` - logout (потребує `[Authorize]`)
- Всі інші protected endpoints автоматично отримають Bearer token

---

### Тестування:

1. **Login** - токени зберігаються автоматично
2. **Protected request** - Bearer token додається автоматично
3. **Token expired** - автоматично refresh і retry
4. **Logout** - викликає `/auth/revoke` і очищає дані

---

### Безпека:

✅ Токени в `SecureStorage` (encrypted on device)  
✅ Автоматичний Bearer token для всіх requests  
✅ Автоматичний refresh при expiration  
✅ Proper logout з revoke на сервері  

---

**Все готово для production! 🚀**
